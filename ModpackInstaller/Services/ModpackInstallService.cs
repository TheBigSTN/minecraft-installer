using System;
using System.IO.Compression;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ModpackInstaller.Models;
using ModpackInstaller.Models.DTOs;
using ModpackInstaller.Models.Interfaces;
using ModpackInstaller.Services.Modpack;
using ModpackInstaller.Models.Modrinth;
using ModpackInstaller.Services.Notifications;

namespace ModpackInstaller.Services;

public static class ModpackInstallService {
    public static async Task<ModpackMetadataStorage> DownloadAndInstallModpackAsync(
        PublicModpackRequestResponse modpack,
        string baseInstallPath,
        bool serverInstall = false,
        bool nonDiscoverableMetadata = false,
        bool useSubfolder = true
    ) {
        var notification = NotificationManager.Show(
            "Downloading modpack...",
            "",
            false
        );
        // 1. Resolve paths
        var installPath = !useSubfolder
            ? baseInstallPath
            : Path.Combine(baseInstallPath, modpack.ModpackName.Trim());

        Directory.CreateDirectory(installPath);

        // FIX: Generate a single ID to be shared between metadata and path registry
        var metadataId = Guid.NewGuid();

        var metadataFilePath = nonDiscoverableMetadata
            ? Path.Combine(installPath, "metadata.json")
            : ModpackMetadataRegistry.GetPath(metadataId);

        // 2. Construct metadata model
        ModpackMetadata metadata = new() {
            Id = metadataId, // Use the consistent ID
            ModpackId = Guid.Parse(modpack.Id),
            InstallPath = installPath,
            Name = modpack.ModpackName,
            GameVersion = modpack.GameVersion,
            Loader = modpack.Loader,
            Author = modpack.AuthorName,
            CreatedAt = modpack.CreatedAt,
            UpdatedAt = modpack.ModifiedAt,
            Description = modpack.Description,
            LoaderVersion = modpack.LoaderVersion,
            VersionId = modpack.LatestVersionId,
            VersionSemver = modpack.LatestVersion,
            Source = ModpackSource.Remote,
            IsServerInstall = serverInstall
        };

        // 3. Execute download and file extraction pipeline
        var zipPath = Path.Combine(installPath, "modpack.zip");

        try {
            await BackendApiService.DownloadVersionAsync(
                modpack.Id,
                modpack.LatestVersionId,
                zipPath).ConfigureAwait(false);

            ZipFile.ExtractToDirectory(zipPath, installPath, true);

            var fileTree = await BackendApiService.GetVersionTreeAsync(modpack.LatestVersionId).ConfigureAwait(false);
            var fileTreePath = ModpackTreeService.GetTreeJsonPath(installPath);

            ModpackTreeService.Save(fileTreePath, fileTree!);
        }
        finally {
            if (File.Exists(zipPath))
                File.Delete(zipPath);
        }

        var storage = new ModpackMetadataStorage(metadataFilePath, metadata);
        await storage.SaveAsync().ConfigureAwait(false);

        notification.Title = "Downloading mods of modpack...";
        notification.ReportProgress(0);

        var manifestService = await ModpackManifestStorage.CreateInstanceAsync(installPath).ConfigureAwait(false);

        await manifestService.FilesystemSyncService.SyncToFileSystemAsync(serverInstall).ConfigureAwait(false);

        return storage;
    }

    public static async Task<bool> UpdateModpackAsync(ModpackMetadataStorage metadataStorage) {
        var metadata = metadataStorage.GetData();

        if (metadata.ModpackId is null)
            return false;

        Directory.CreateDirectory(metadata.InstallPath);

        var treeService = new ModpackTreeService(metadata.InstallPath);

        // 1. Fetch independent remote data in parallel
        var remoteMetadataTask = BackendApiService.GetModpack(metadata.ModpackId.Value);
        var oldManifestTask = BackendApiService.GetModpackManifestAsync(metadata.VersionId);

        await Task.WhenAll(remoteMetadataTask, oldManifestTask).ConfigureAwait(false);

        var remoteMetadata = await remoteMetadataTask.ConfigureAwait(false) ?? throw new Exception("Modpack not found.");
        var oldManifest = await oldManifestTask.ConfigureAwait(false);

        // 2. Fetch the new version data in parallel using the remote metadata ID
        var latestVersionId = remoteMetadata.LatestVersion.Id;
        var remoteTreeTask = BackendApiService.GetVersionTreeAsync(latestVersionId);
        var newManifestTask = BackendApiService.GetModpackManifestAsync(latestVersionId);

        await Task.WhenAll(remoteTreeTask, newManifestTask).ConfigureAwait(false);

        var remoteTree = await remoteTreeTask.ConfigureAwait(false) ?? throw new Exception("Failed to download modpack tree.");
        var newManifest = await newManifestTask.ConfigureAwait(false);

        if (oldManifest is null || newManifest is null)
            throw new Exception("Failed to download modpack manifests.");

        // 3. Process tree diffs and clean up deleted files
        var diff = ModpackTreeDiff.Create(treeService.ModpackTree, remoteTree);

        foreach (var fullPath in diff.FilesToDelete.Select(file => Path.Combine(metadata.InstallPath, file.FilePath))) {
            if (File.Exists(fullPath))
                File.Delete(fullPath);

            RemoveEmptyDirectories(Path.GetDirectoryName(fullPath));
        }

        // 4. Download new/modified files
        foreach (var file in diff.FilesToDownload) {
            if (file.FilePath == "manifest.json")
                continue;

            var destination = Path.Combine(metadata.InstallPath, file.FilePath);
            Directory.CreateDirectory(Path.GetDirectoryName(destination)!);

            await BackendApiService.DownloadStoredFileAsync(
                file.Sha256,
                destination).ConfigureAwait(false);
        }

        treeService.ModpackTree = remoteTree;
        treeService.Save();

        // 5. Handle mod installation changes via manifest diff
        var store = await ModpackManifestStorage.CreateInstanceAsync(metadata.InstallPath).ConfigureAwait(false);
        var installationManager = new ModInstallationManager(store);
        var manifestDiff = ManifestDiffCreator.Create(oldManifest, newManifest);

        foreach (var mod in manifestDiff.Removed) {
            installationManager.RemoveMod(mod);
        }

        foreach (var mod in manifestDiff.Updated) {
            await installationManager.InstallModAsync(mod.NewMod, true).ConfigureAwait(false);
        }

        foreach (var mod in manifestDiff.Added) {
            await installationManager.InstallModAsync(mod, true).ConfigureAwait(false);
        }

        // 6. Persist updated metadata
        metadataStorage.Update(local => {
            local.VersionId = latestVersionId;
            local.VersionSemver = remoteMetadata.LatestVersion.Semver;
        });

        return true;
    }

    private static void RemoveEmptyDirectories(string? directory) {
        while (!string.IsNullOrEmpty(directory) &&
               Directory.Exists(directory) &&
               !Directory.EnumerateFileSystemEntries(directory).Any()) {
            Directory.Delete(directory);
            directory = Path.GetDirectoryName(directory);
        }
    }
}