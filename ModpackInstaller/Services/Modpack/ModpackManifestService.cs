using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using DynamicData;
using ModpackInstaller.Infrastructure;
using ModpackInstaller.Models;
using ModpackInstaller.Models.Modrinth;

namespace ModpackInstaller.Services.Modpack;

public class ModpackManifestService {
    private string? ManifestPath {
        get {
            if (!string.IsNullOrEmpty(_installPath))
                return Path.Combine(_installPath, "manifest.json");
            return null;
        }
    }
    private string? _installPath;

    public ModpackManifest Manifest { get; private set; }

    public ModpackManifestService(string modpackInstallPath) {
        _installPath = modpackInstallPath;
        Manifest = new ModpackManifest();
        Load();
    }

    public ModpackManifestService() {
        Manifest = new ModpackManifest();
    }

    public void OpenModpack(string modpackInstallPath) {
        if(_installPath != modpackInstallPath){
            _installPath = modpackInstallPath;
        }
        Load();
    }

    //public static ModpackManifest Load(string manifestPath) {
    //    if (!File.Exists(manifestPath)) {
    //        return new ModpackManifest();
    //    }

    //    try {
    //        var json = File.ReadAllText(manifestPath);
    //        ModpackManifest modpackManifest = JsonSerializer.Deserialize<ModpackManifest>(json) ?? new ModpackManifest();
    //        return modpackManifest;
    //    }
    //    catch {
    //        return new ModpackManifest();
    //    }
    //}


    [MemberNotNull(nameof(Manifest))]
    public ModpackManifest Load() {
        if (!File.Exists(ManifestPath)) {
            Manifest = new ModpackManifest();
            return Manifest;
        }

        try {
            var json = File.ReadAllText(ManifestPath);
            ModpackManifest modpackManifest = JsonSerializer.Deserialize<ModpackManifest>(json) ?? new ModpackManifest();
            Manifest = modpackManifest;
            return modpackManifest;
        }
        catch {
            Manifest = new ModpackManifest();
            return new ModpackManifest();
        }
    }

    public async Task<ModpackManifest> LoadSync() {
        if(!File.Exists(ManifestPath)) {
            Manifest = new ModpackManifest();
            return Manifest;
        }

        try {
            var json = File.ReadAllText(ManifestPath);
            ModpackManifest modpackManifest = JsonSerializer.Deserialize<ModpackManifest>(json) ?? new ModpackManifest();
            Manifest = modpackManifest;
            await SyncWithFilesystemAsync();
            return modpackManifest;
        } catch {
            Manifest = new ModpackManifest();
            return new ModpackManifest();
        }
    }

    public static void Save(ModpackManifest modpackManifest, string manifestPath) {
        // Ne asigurăm că folderul există înainte de scriere
        var directory = Path.GetDirectoryName(manifestPath);
        if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);

        var json = JsonSerializer.Serialize(modpackManifest, AppVariables.DefaultJsonOptions);
        File.WriteAllText(manifestPath, json);
    }

    public void Save() {
        if (Manifest == null || ManifestPath == null)
            return;

        // Ne asigurăm că folderul există înainte de scriere
        var directory = Path.GetDirectoryName(ManifestPath);
        if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);

        var json = JsonSerializer.Serialize(Manifest, AppVariables.DefaultJsonOptions);
        File.WriteAllText(ManifestPath, json);
    }

    public void ParseAllMods(Action<InstalledModInfo> action) {
        if(Manifest == null)
            return;

        foreach (InstalledModInfo modInfo in Manifest.InstalledMods) {
            action.Invoke(modInfo);
        }
        Save();
    }



    public bool AddOrUpdateMod(InstalledModInfo modInfo, bool modpackInstall) {
        if(Manifest == null)
            return false;

        var existing = Manifest.InstalledMods
            .FirstOrDefault(m => m.ProjectId == modInfo.ProjectId);

        if (existing == null) {
            if (modpackInstall)
                modInfo.Source = ModSource.Remote;

            Manifest.InstalledMods.Add(modInfo);
            Save();
            return true; // added
        }

        if (existing.VersionId == modInfo.VersionId) {
            return false; // same version, nothing changed
        }

        // Different version → update
        RemoveMod(existing.ProjectId);

        if (modpackInstall)
            modInfo.Source = ModSource.Remote;

        Manifest.InstalledMods.Add(modInfo);

        Save();
        return true; // updated
    }

    public void AddMod(InstalledModInfo installedModInfo, bool modpackInstall) {
        if(Manifest == null)
            return;

        if (modpackInstall) installedModInfo.Source = ModSource.Remote;
        Manifest.InstalledMods.Add(installedModInfo);
        Save();
    }

    public InstalledModInfo? AddMod(ModrinthProject project, ModrinthVersion version) {
        if (Manifest == null)
            return null;

        // Verificăm dacă există deja în lista de tip InstalledModInfo
        if (Manifest.InstalledMods.Any(m => m.ProjectId == project.Id))
            return null;

        var file = version.PrimaryFile;
        if (file == null && version.Files.FirstOrDefault() == null) return null;
        else file ??= version.Files.FirstOrDefault();

        // AICI: Creăm obiectul nou pentru listă
        var newItem = new InstalledModInfo {
            ProjectId = project.Id,
            VersionId = version.Id,
            Title = project.Title,
            Filename = file?.Filename ?? "",
            DownloadUrl = file?.Url ?? "",
            IconUrl = project.IconURL,
            ClientSide = project.ClientSide,
            ServerSide = project.ServerSide,
            Source = ModSource.Local,
            Enabled = true
        };

        Manifest.InstalledMods.Add(newItem);
        Save();
        return newItem;
    }

    public void RemoveMod(string projectId) {
        if(Manifest == null || _installPath == null)
            return;

        var modToDelete = Manifest.InstalledMods.FirstOrDefault(m => m.ProjectId == projectId);

        if (modToDelete != null) {
            try {
                // 2. Construim calea către fișierul .jar
                // Presupunem că 'FileName' este proprietatea care reține numele fișierului salvat
                string fullPath = Path.Combine(_installPath, "mods", modToDelete.Filename);

                // 3. Ștergem fișierul de pe disc dacă există
                if (File.Exists(fullPath)) {
                    File.Delete(fullPath);
                }
            }
            catch (Exception ex) {
                // Loghează eroarea dacă fișierul este blocat de Minecraft sau sistem
                Debug.WriteLine($"Nu s-a putut șterge fișierul: {ex.Message}");
            }

            // 4. Îl eliminăm din manifest și salvăm modificarea
            Manifest.InstalledMods.Remove(modToDelete);
            Save();
        }
    }

    public bool IsModInstalled(InstalledModInfo modInfo) {
        var state = GetModInstallState(modInfo);
        return state == ModInstallState.InstalledSameVersion
            || state == ModInstallState.InstalledDifferentVersion;
    }

    public bool IsModInstalledDiferentVersion(InstalledModInfo modInfo) {
        return GetModInstallState(modInfo) == ModInstallState.InstalledDifferentVersion;
    }

    public bool IsModInstalledSameVersion(InstalledModInfo modInfo) {
        return GetModInstallState(modInfo) == ModInstallState.InstalledSameVersion;
    }

    public ModInstallState GetModInstallState(InstalledModInfo modInfo) {
        if(Manifest == null)
            return ModInstallState.InstalledDifferentVersion;

        var mod = Manifest.InstalledMods
            .FirstOrDefault(m => m.ProjectId == modInfo.ProjectId);

        if (mod == null)
            return ModInstallState.NotInstalled;

        if (mod.VersionId == modInfo.VersionId)
            return ModInstallState.InstalledSameVersion;

        return ModInstallState.InstalledDifferentVersion;
    }

    public static async Task<bool> DownloadModAsync(InstalledModInfo modInfo, string installPath) {
        try {
            var modsFolder = Path.Combine(installPath, "mods");
            if (!Directory.Exists(modsFolder))
                Directory.CreateDirectory(modsFolder);

            var filePath = Path.Combine(modsFolder, modInfo.Filename);

            using var client = new HttpClient();
            var data = await client.GetByteArrayAsync(modInfo.DownloadUrl);
            await File.WriteAllBytesAsync(filePath, data);

            Debug.WriteLine($"[Download] Finalizat: {modInfo.Title}");
            return true;
        }
        catch (Exception ex) {
            Debug.WriteLine($"[Error] Descărcare eșuată pentru {modInfo.Title}: {ex.Message}");
            return false;
        }
    }

    public async Task SyncWithFilesystemAsync() {
        if(_installPath == null || Manifest == null)
            return;

        var modsFolder = Path.Combine(_installPath, "mods");
        if(!Directory.Exists(modsFolder))
            return;

        var jarFiles = Directory.GetFiles(modsFolder, "*.jar");

        foreach(var filePath in jarFiles) {
            var fileName = Path.GetFileName(filePath);

            // 🔹 deja în manifest?
            if(Manifest.InstalledMods.Any(m => m.Filename == fileName))
                continue;

            try {
                // 1️⃣ hash
                var sha1 = HashUtils.ComputeSHA1(filePath);

                // 2️⃣ query Modrinth
                var version = await ModrinthApiService.GetVersionByHashAsync(sha1);

                if(version != null) {
                    // 3️⃣ avem match → luăm și project
                    var project = await ModrinthApiService.GetProjectAsync(version.ProjectId);

                    var primaryFile = version.Files.FirstOrDefault(f => f.Hashes.Sha1 == sha1)
                                      ?? version.Files.FirstOrDefault();

                    var mod = new InstalledModInfo {
                        ProjectId = version.ProjectId,
                        VersionId = version.Id,
                        Title = project?.Title ?? fileName,
                        Filename = fileName,
                        DownloadUrl = primaryFile?.Url ?? "",
                        IconUrl = project?.IconURL ?? "",
                        Source = ModSource.Remote,
                        Enabled = true,
                        ClientSide = project?.ClientSide ?? SideSupport.unknown,
                        ServerSide = project?.ServerSide ?? SideSupport.unknown
                    };

                    Manifest.InstalledMods.Add(mod);
                } else {
                    // 4️⃣ fallback → LOCAL MOD
                    var (title, versionStr) = ParseFileName(fileName);

                    var mod = new InstalledModInfo {
                        ProjectId = Guid.NewGuid().ToString(), // local id
                        VersionId = versionStr,
                        Title = title,
                        Filename = fileName,
                        Source = ModSource.Local,
                        Enabled = true
                    };

                    Manifest.InstalledMods.Add(mod);
                }
            } catch(Exception ex) {
                Debug.WriteLine($"[Sync Error] {fileName}: {ex.Message}");
            }
        }

        Save();
    }

    private static (string title, string version) ParseFileName( string fileName ) {
        var name = Path.GetFileNameWithoutExtension(fileName);

        // ex: jei-1.20.1-15.2.0.27
        var parts = name.Split('-', StringSplitOptions.RemoveEmptyEntries);

        if(parts.Length == 0)
            return (name, "");

        // heuristic:
        string title = parts[0];
        string version = parts.Length > 1 ? parts[^1] : "";

        // beautify title
        title = title.Replace("_", " ");
        title = char.ToUpper(title[0]) + title.Substring(1);

        return (title, version);
    }



}


public enum ModInstallState {
    NotInstalled,
    InstalledSameVersion,
    InstalledDifferentVersion
}