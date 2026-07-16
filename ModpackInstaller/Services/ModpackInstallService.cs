using System;
using System.IO.Compression;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ModpackInstaller.Models;
using ModpackInstaller.Models.DTOs;
using ModpackInstaller.Services.Modpack;
using ModpackInstaller.Models.Modrinth;

namespace ModpackInstaller.Services;

public static class ModpackInstallService {
    private static async Task InstallModsOfModpack( 
            ModpackMetadata modpackMetadata,
            bool serverInstall = false
        ) {
        ModpackManifestService manifestService = ModpackManifestService.CreateInstance(modpackMetadata.InstallPath);
        var manifest = manifestService.GetManifest();
        
        var downloadTasks = manifest.InstalledMods.Select(async mod => {
            bool shouldDownload;

            if(serverInstall) {
                shouldDownload = mod.ServerSide is SideSupport.required or SideSupport.optional;
            } else {
                shouldDownload = mod.ClientSide is SideSupport.required or SideSupport.optional;
            }

			if(shouldDownload) {
				await ModpackManifestService.DownloadModAsync(mod, modpackMetadata.InstallPath);
			}
        });

        await Task.WhenAll(downloadTasks);
    }

    public static async Task<ModpackMetadata> DownloadAndInstallModpack(
		    PublicModpackRequestResponse modpack,
		    string baseInstallPath,
            bool serverInstall = false,
			bool nonDiscoverableMedatadata = false,
			bool useSubfolder = true
        ) {
		var installPath = !useSubfolder
            ? baseInstallPath
            : Path.Combine(baseInstallPath, modpack.ModpackName.Trim());

		Directory.CreateDirectory(installPath);

		ModpackMetadata metadata = new() {
			Id = Guid.NewGuid(),
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
			// ModpackPassword = null,
			// SharingCode = null,
			VersionId = modpack.LatestVersionId,
			VersionSemver = modpack.LatestVersion,
			Source = ModpackSource.Remote,
            IsServerInstall = serverInstall
        };
		
        var zipPath = Path.Combine(installPath, "modpack.zip");

		try {
			await BackendApiService.DownloadVersionAsync(
				modpack.Id,
				modpack.LatestVersionId,
				zipPath);

            ZipFile.ExtractToDirectory(zipPath, installPath, true);

            var fileTree = await BackendApiService.GetVersionTreeAsync(modpack.LatestVersionId);

            var fileTreePath = ModpackTreeService.GetTreeJsonPath(installPath);
            
            ModpackTreeService.Save(fileTreePath, fileTree!);

		} finally {
			if (File.Exists(zipPath))
				File.Delete(zipPath);
		}

		if(nonDiscoverableMedatadata) {
			ModpackMedatataService registry = new(installPath);

			registry.Create(metadata);
		} else {
			ModpackMedatataService registry = new();

			registry.Create(metadata);
        }

		await InstallModsOfModpack(metadata, serverInstall);
		return metadata;
	}

	public static async Task<ModpackMetadata?> UpdateModpack(ModpackMetadata metadata) {
		if (metadata.ModpackId is null)
			return null;
		
		if (!Directory.Exists(metadata.InstallPath))
			Directory.CreateDirectory(metadata.InstallPath);

		var treeService = new ModpackTreeService(metadata.InstallPath);

		var remoteMetadata = await BackendApiService.GetModpack(metadata.ModpackId.Value, null)
		                     ?? throw new Exception("Modpack not found.");

		var remoteTree = await BackendApiService.GetVersionTreeAsync(remoteMetadata.LatestVersion.Id)
		                 ?? throw new Exception("Failed to download modpack tree.");
		
		var oldManifest = await BackendApiService.GetModpackManifestAsync(metadata.VersionId);
		var newManifest = await BackendApiService.GetModpackManifestAsync(remoteMetadata.LatestVersion.Id);
		
		if (oldManifest is null || newManifest is null) 
			throw new Exception("Failed to download modpack manifests.");

		var diff = ModpackTreeDiff.Create(treeService.ModpackTree, remoteTree);

		// Șterge fișierele eliminate
		foreach (var file in diff.FilesToDelete) {
			var fullPath = Path.Combine(metadata.InstallPath, file.FilePath);

			if (File.Exists(fullPath))
				File.Delete(fullPath);

			RemoveEmptyDirectories(Path.GetDirectoryName(fullPath));
		}

		// Descarcă fișierele noi/modificate
		foreach (var file in diff.FilesToDownload) {
			var destination = Path.Combine(metadata.InstallPath, file.FilePath);

			if (file.FilePath == "manifest.json") {
				continue;
			}

			Directory.CreateDirectory(Path.GetDirectoryName(destination)!);

			await BackendApiService.DownloadStoredFileAsync(
				file.Sha256,
				destination);
		}

		treeService.ModpackTree = remoteTree;
		treeService.Save();
		
		var manifestService = ModpackManifestService.CreateInstance(metadata.InstallPath);
		var manifestDiff = ManifestDiffCreator.Create(oldManifest, newManifest);

		foreach (var mod in manifestDiff.Removed) {
			manifestService.RemoveMod(mod);
		}

		foreach (var mod in manifestDiff.Updated) {
			await manifestService.InstallModAsync(mod.NewMod, true);
		}

		foreach (var mod in manifestDiff.Added) {
			await manifestService.InstallModAsync(mod, true);
		}

		metadata.VersionId = remoteMetadata.LatestVersion.Id;
		metadata.VersionSemver = remoteMetadata.LatestVersion.Semver;
		new ModpackMedatataService().Save(metadata);
		return metadata;
	}

	private static void RemoveEmptyDirectories(string? directory) {
		while (!string.IsNullOrEmpty(directory) &&
		       Directory.Exists(directory) &&
		       !Directory.EnumerateFileSystemEntries(directory).Any())
		{
			Directory.Delete(directory);
			directory = Path.GetDirectoryName(directory);
		}
	}
}

