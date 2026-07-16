using System;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using ModpackInstaller.Infrastructure;
using ModpackInstaller.Models;
using ModpackInstaller.Models.Modrinth;

namespace ModpackInstaller.Services.Modpack;

public class ModpackManifestService {
    private string? ManifestPath => string.IsNullOrEmpty(_installPath) 
        ? null 
        : Path.Combine(_installPath, "manifest.json");
    
    private readonly string? _installPath;

    public readonly ObservableList<ModInfo> InstalledMods;
    private readonly ConcurrentDictionary<string, Lazy<Task>> _installQueue = new();

    private ModpackManifestService(string modpackInstallPath) {
        InstalledMods = [];
        _installPath = modpackInstallPath;
        Load();
    }
    
    private static readonly ConcurrentDictionary<string, ModpackManifestService> Instances = new();

    public static ModpackManifestService CreateInstance(string modpackInstallPath) {
        modpackInstallPath = Path.GetFullPath(modpackInstallPath);

        var service = Instances.GetOrAdd(
            modpackInstallPath,
            path => new ModpackManifestService(path));
        service.Load();

        return service;
    }
    
    public static bool ReleaseInstance(ModpackManifestService service) {
        if (service._installPath is null)
            return false;

        return Instances.TryRemove(
            Path.GetFullPath(service._installPath),
            out _);
    }
    
    public static bool ReleaseInstance(string installPath) {
        return Instances.TryRemove(
            installPath,
            out _);
    }

    private void Load() {
        if (!File.Exists(ManifestPath)) {
            InstalledMods.Clear();
            return;
        }

        try {
            var json = File.ReadAllText(ManifestPath);
            var modpackManifest = JsonSerializer.Deserialize<ModpackManifest>(json) ?? new ModpackManifest();
            InstalledMods.Clear();
            InstalledMods.AddRange(modpackManifest.InstalledMods);
        }
        catch {
            // ignored
        }
    }

    // public void ChangeModpack(string modpackPath) {
    //     _installPath = modpackPath;
    //     
    //     Load();
    // }

    public ModpackManifest GetManifest() {
        return new ModpackManifest {
            InstalledMods = InstalledMods.ToList()
        };
    }

    public async Task LoadSync( bool serverInstall ) {
        if(!File.Exists(ManifestPath)) {
            return;
        }

        try {
            var json = await File.ReadAllTextAsync(ManifestPath);
            var modpackManifest = JsonSerializer.Deserialize<ModpackManifest>(json) ?? new ModpackManifest();
            InstalledMods.Clear();
            InstalledMods.AddRange(modpackManifest.InstalledMods);
            await SyncWithFilesystemAsync();
            await SyncToFileSistemAsync(serverInstall);
        }
        catch {
            // ignored
        }
    }
    
    private readonly object _fileLock = new();

    private void Save() {
        if (ManifestPath == null)
            return;

        lock (_fileLock) {
            
            var directory = Path.GetDirectoryName(ManifestPath);
            if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);

            var manifest = GetManifest();

            var json = JsonSerializer.Serialize(manifest, AppVariables.DefaultJsonOptions);
            
            File.WriteAllText(ManifestPath, json);
        }

    }

    public async Task InstallModAsync(IModVersion searchInfo, bool updateIfExisting) {
        var lazyTask = _installQueue.GetOrAdd(
            searchInfo.ProjectId,
            _ => new Lazy<Task>(() => InstallModInternalAsync(searchInfo, updateIfExisting)));

        try
        {
            await lazyTask.Value;
            Save();
        }
        finally
        {
            _installQueue.TryRemove(searchInfo.ProjectId, out _);
        }
        
        Save();
    }

    private async Task InstallModInternalAsync(IModVersion searchInfo, bool updateIfExisting) {
        if(_installPath == null) return;

        var existing = InstalledMods
            .FirstOrDefault(m => m.ProjectId == searchInfo.ProjectId);

        if (!updateIfExisting && existing != null) return;

        if (existing?.VersionId == searchInfo.VersionId) return;

        var project = await ModrinthApiService.GetProjectAsync(searchInfo.ProjectId);
        if (project == null) return;

        var version = await ModrinthApiService.GetVersionAsync(searchInfo.VersionId);
        if (version == null) return;

        var modsFolder = Path.Combine(_installPath, "mods");
        if (existing != null) {
            var oldPath = Path.Combine(modsFolder, existing.Filename);
            if(File.Exists(oldPath))
                File.Delete(oldPath);
        }
        
        var file = version.PrimaryFile ?? version.Files.FirstOrDefault();
        if (file == null) return;

        var projectMembers = await ModrinthApiService.GetProjectMembersAsync(project.Id);
        
        var newModInfo = new ModInfo {
            ProjectId = project.Id,
            VersionId = version.Id,
            VersionNumber = version.VersionNumber,
            Title = project.Title,
            Filename = file.Filename,
            DownloadUrl = file.Url,
            FileSha = file.Hashes.Sha1,
            IconUrl = project.IconURL,
            ClientSide = project.ClientSide,
            ServerSide = project.ServerSide,
            Source = ModSource.Remote,
            Enabled = true,
            OwnerName = projectMembers.FirstOrDefault()?.User.Username ?? "Unknown"
        };

        foreach (var modrinthDependency in version.Dependencies
                     .Where(modrinthDependency => modrinthDependency.DependencyType == DependencyType.Required)) {
            await InstallModAsync(modrinthDependency, updateIfExisting);
        }
        
        await DownloadModAsync(newModInfo, _installPath);
        
        if(existing == null) 
            InstalledMods.Add(newModInfo);
        else {
            InstalledMods[InstalledMods.IndexOf(existing)] = newModInfo;
        }
    }

    public bool RemoveMod(ModInfo mod) {
        if (_installPath == null)
            return false;

        try {
            var filePath = Path.Combine(_installPath, "mods", mod.Filename);

            if (File.Exists(filePath))
                File.Delete(filePath);

            var existing = InstalledMods.FirstOrDefault(x => x.ProjectId == mod.ProjectId);

            if (existing != null)
                InstalledMods.Remove(existing);

            Save();

            return true;
        }
        catch {
            return false;
        }
    }

    public void EnableDisableMod(string projectId, bool status) {
        if (InstalledMods.FirstOrDefault(m => m.ProjectId == projectId) is null || _installPath == null)
            return;
        
        var mod = InstalledMods.First(m => m.ProjectId == projectId);
        
        if (mod.Enabled == status)
            return;
        
        var filePath = Path.Combine(_installPath, "mods", mod.Filename);
        var deactivationPath = filePath + ".deactivation";

        try {
            if (status) {
                // Enable: Rename from .deactivation back to .jar
                if (File.Exists(deactivationPath)) {
                    File.Move(deactivationPath, filePath, true);
                }
            } else {
                // Disable: Rename from .jar to .deactivation
                if (File.Exists(filePath)) {
                    File.Move(filePath, deactivationPath, true);
                }
            }
        }
        catch (Exception ex) {
            Console.WriteLine($"Failed to toggle mod file state: {ex.Message}");
        }
        
        mod.Enabled = status;
        Save();
    }
    
    public bool IsModInstalled(IModVersion modInfo) => 
        GetModInstallState(modInfo) is ModInstallState.InstalledSameVersion or ModInstallState.InstalledDifferentVersion;

    private ModInstallState GetModInstallState(IModVersion modInfo) {
        var mod = InstalledMods
            .FirstOrDefault(m => m.ProjectId == modInfo.ProjectId);

        if (mod == null)
            return ModInstallState.NotInstalled;

        return mod.VersionId == modInfo.VersionId
            ? ModInstallState.InstalledSameVersion
            : ModInstallState.InstalledDifferentVersion;
    }

    public static async Task DownloadModAsync(ModInfo modInfo, string modpackInstallPath) {
        var modsFolder = Path.Combine(modpackInstallPath, "mods");
        Directory.CreateDirectory(modsFolder);

        var filePath = Path.Combine(modsFolder, modInfo.Filename);
        var tempPath = filePath + ".tmp";

        try {
            await using var input =
                await WebService.Client.GetStreamAsync(modInfo.DownloadUrl);

            await using var output = File.Create(tempPath);

            await input.CopyToAsync(output);

            output.Close();

            File.Move(tempPath, filePath, true);
        }
        finally {
            if (File.Exists(tempPath))
                File.Delete(tempPath);
        }
    }

    public async Task SyncWithFilesystemAsync() {
        if(_installPath == null)
            return;

        var modsFolder = Path.Combine(_installPath, "mods");
        if(!Directory.Exists(modsFolder))
            return;

        var jarFiles = Directory.GetFiles(modsFolder, "*.jar");

        foreach(var filePath in jarFiles) {
            var fileName = Path.GetFileName(filePath);

            // 🔹 deja în manifest?
            if(InstalledMods.Any(m => m.Filename == fileName))
                continue;

            try {
                var sha1 = HashUtils.ComputeSha1(filePath);

                var version = await ModrinthApiService.GetVersionByHashAsync(sha1);

                if(version != null) {
                    var existingMod = InstalledMods
                        .FirstOrDefault(m => m.ProjectId == version.ProjectId);

                    if(existingMod != null) {
                        if(existingMod.VersionId == version.Id)
                            continue;

                        try {
                            File.Delete(filePath);

                            Console.WriteLine(
                                $"Removed duplicate version: {fileName} " +
                                $"(manifest={existingMod.VersionId}, found={version.Id})");
                        } catch(Exception ex) {
                            Console.WriteLine($"Failed to delete {fileName}: {ex.Message}");
                        }

                        continue;
                    }
                    
                    await InstallModAsync(new ModVersion(version.ProjectId, version.Id), false);
                } else {
                    var (title, versionStr) = ParseFileName(fileName);

                    var mod = new ModInfo {
                        ProjectId = Guid.NewGuid().ToString(),
                        VersionId = versionStr,
                        VersionNumber = versionStr,
                        Title = title,
                        Filename = fileName,
                        Source = ModSource.Local,
                        Enabled = true,
                        OwnerName = "Unknown"
                    };

                    InstalledMods.Add(mod);
                }
            } catch(Exception ex) {
                Console.WriteLine($"[Sync Error] {fileName}: {ex.Message}");
            }
        }

        Save();
    }

    public async Task SyncToFileSistemAsync(bool serverInstall) {
        if (_installPath == null)
            return;

        var modsFolder = Path.Combine(_installPath, "mods");

        foreach (var mod in InstalledMods.ToList()) {

            if (!ShouldDownload(mod, serverInstall))
                continue;

            var filePath = Path.Combine(modsFolder, mod.Filename);
            var disabledPath = filePath + ".deactivation";

            if (File.Exists(disabledPath))
                continue;

            if (!File.Exists(filePath)) {
                await DownloadModAsync(mod, _installPath);
                continue;
            }

            if (mod.Source != ModSource.Remote ||
                await IsValidModAsync(mod, filePath)) continue;
            
            File.Delete(filePath);
            await DownloadModAsync(mod, _installPath);
        }

        Save();
    }

    private async Task<bool> IsValidModAsync( ModInfo mod, string filePath ) {
        if(mod.Source != ModSource.Remote)
            return true;

        if(!string.IsNullOrWhiteSpace(mod.FileSha)) {
            return await CompareLocalFileAsync(filePath, mod.FileSha);
        }

        var version = await ModrinthApiService.GetVersionAsync(mod.VersionId);
        if(version == null)
            return true;

        var file = version.Files.FirstOrDefault(f => f.Filename == mod.Filename)
                   ?? version.PrimaryFile;

        if(file?.Hashes.Sha1 == null)
            return true;

        mod.FileSha = file.Hashes.Sha1.ToLowerInvariant();

        var index = InstalledMods.IndexOf(mod);
        
        if (index != -1) {
            InstalledMods[index].FileSha = mod.FileSha;
        }

        // 3. compare
        return await CompareLocalFileAsync(filePath, mod.FileSha);
    }

    private static async Task<bool> CompareLocalFileAsync( string filePath, string expectedSha1 ) {
        await using var stream = new FileStream(
            filePath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            bufferSize: 1024 * 1024, // 1MB buffer (important)
            options: FileOptions.Asynchronous | FileOptions.SequentialScan
        );

        using var sha1 = System.Security.Cryptography.SHA1.Create();

        var hash = await sha1.ComputeHashAsync(stream);
        var local = Convert.ToHexString(hash).ToLowerInvariant();

        return local == expectedSha1.ToLowerInvariant();
    }

    private static (string title, string version) ParseFileName( string fileName ) {
        var name = Path.GetFileNameWithoutExtension(fileName);

        // ex: jei-1.20.1-15.2.0.27
        var parts = name.Split('-', StringSplitOptions.RemoveEmptyEntries);

        if(parts.Length == 0)
            return (name, "");

        // heuristic:
        var title = parts[0];
        var version = parts.Length > 1 ? parts[^1] : "";

        // beautify title
        title = title.Replace("_", " ");
        title = char.ToUpper(title[0]) + title.Substring(1);

        return (title, version);
    }
    
    private static bool ShouldDownload(ModInfo mod, bool serverInstall) {
        return serverInstall
            ? mod.ServerSide is SideSupport.required or SideSupport.optional
            : mod.ClientSide is SideSupport.required or SideSupport.optional;
    }   
    
}


public enum ModInstallState {
    NotInstalled,
    InstalledSameVersion,
    InstalledDifferentVersion
}

public interface IModVersion {
    string ProjectId { get; }
    string VersionId { get; }
}

public class ModVersion(string projectId, string versionId) : IModVersion {
    public string ProjectId { get; } = projectId;
    public string VersionId { get; } = versionId;
}