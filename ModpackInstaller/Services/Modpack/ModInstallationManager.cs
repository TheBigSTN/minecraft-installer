using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using ModpackInstaller.Infrastructure;
using ModpackInstaller.Models;
using ModpackInstaller.Models.Modrinth;

namespace ModpackInstaller.Services.Modpack;

public class ModInstallationManager(ModpackManifestStorage store) {
    private readonly ConcurrentDictionary<string, Lazy<Task>> _installQueue = new();

    public async Task InstallModAsync(IModVersion searchInfo, bool updateIfExisting) {
        var lazyTask = _installQueue.GetOrAdd(
            searchInfo.ProjectId,
            _ => new Lazy<Task>(() => InstallModInternalAsync(searchInfo, updateIfExisting)));

        try {
            await lazyTask.Value.ConfigureAwait(false);
            store.MarkDirty();
        }
        finally {
            _installQueue.TryRemove(searchInfo.ProjectId, out _);
        }
        
        store.MarkDirty();
    }

    private async Task InstallModInternalAsync(IModVersion searchInfo, bool updateIfExisting) {
        var existing = store.InstalledMods.FirstOrDefault(m => m.ProjectId == searchInfo.ProjectId);
        if (!updateIfExisting && existing != null) return;
        if (existing?.VersionId == searchInfo.VersionId) return;

        var project = await ModrinthApiService.GetProjectAsync(searchInfo.ProjectId).ConfigureAwait(false);
        if (project == null) return;

        var version = await ModrinthApiService.GetVersionAsync(searchInfo.VersionId).ConfigureAwait(false);
        if (version == null) return;

        var modsFolder = Path.Combine(store.InstallPath, "mods");
        if (existing != null) {
            var oldPath = Path.Combine(modsFolder, existing.Filename);
            if (File.Exists(oldPath)) File.Delete(oldPath);
        }
        
        var file = version.PrimaryFile ?? version.Files.FirstOrDefault();
        if (file == null) return;

        var projectMembers = await ModrinthApiService.GetProjectMembersAsync(project.Id).ConfigureAwait(false);
        
        var newModInfo = new ModInfo {
            ProjectId = project.Id,
            VersionId = version.Id,
            VersionNumber = version.VersionNumber,
            Title = project.Title,
            Filename = file.Filename,
            DownloadUrl = file.Url,
            FileSha = file.Hashes.Sha1,
            IconUrl = project.IconURL,
            Environment = version.Environment,
            Source = ModSource.Remote,
            Enabled = true,
            OwnerName = projectMembers.FirstOrDefault()?.User.Username ?? "Unknown"
        };

        foreach (var dependency in version.Dependencies.Where(d => d.DependencyType == DependencyType.Required)) {
            await InstallModAsync(dependency, updateIfExisting).ConfigureAwait(false);
        }
        
        await DownloadModAsync(newModInfo, store.InstallPath).ConfigureAwait(false);
        
        if (existing == null) {
            store.InstalledMods.Add(newModInfo);
        } else {
            store.InstalledMods[store.InstalledMods.IndexOf(existing)] = newModInfo;
        }
    }

    public void RemoveMod(ModInfo mod) {
        try {
            var filePath = Path.Combine(store.InstallPath, "mods", mod.Filename);
            if (File.Exists(filePath)) File.Delete(filePath);

            var existing = store.InstalledMods.FirstOrDefault(x => x.ProjectId == mod.ProjectId);
            if (existing != null) store.InstalledMods.Remove(existing);

            store.MarkDirty();
        }
        catch {
            // ignored
        }
    }

    public static async Task DownloadModAsync(ModInfo modInfo, string modpackInstallPath) {
        var modsFolder = Path.Combine(modpackInstallPath, "mods");
        Directory.CreateDirectory(modsFolder);
        
        if(modInfo.Source != ModSource.Remote) return;

        var filePath = Path.Combine(modsFolder, modInfo.Filename);
        var tempPath = filePath + ".tmp";

        try {
            var input = await WebService.Client.GetStreamAsync(modInfo.DownloadUrl).ConfigureAwait(false);
            await using var input1 = input.ConfigureAwait(false);
            var output = File.Create(tempPath);
            await using var output1 = output.ConfigureAwait(false);
            await input.CopyToAsync(output).ConfigureAwait(false);
            output.Close();

            File.Move(tempPath, filePath, true);
        }
        catch {
            Console.Write(JsonSerializer.Serialize(modInfo, AppVariables.DefaultJsonOptions));
            Console.WriteLine(modInfo.DownloadUrl);
        }
        finally {
            if (File.Exists(tempPath)) File.Delete(tempPath);
        }
    }
}