using System;
using System.Collections.Generic;
using System.Diagnostics;
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
    private readonly string _manifestPath;
    private readonly string _installPath;

    public ModpackManifest Manifest { get; private set; }

    public ModpackManifestService(string modpackInstallPath) {
        _installPath = modpackInstallPath;
        _manifestPath = Path.Combine(modpackInstallPath, "manifest.json");
        Manifest = Load();
    }

    public static ModpackManifest Load(string manifestPath) {
        if (!File.Exists(manifestPath)) {
            return new ModpackManifest();
        }

        try {
            var json = File.ReadAllText(manifestPath);
            ModpackManifest modpackManifest = JsonSerializer.Deserialize<ModpackManifest>(json) ?? new ModpackManifest();
            return modpackManifest;
        }
        catch {
            return new ModpackManifest();
        }
    }

    public ModpackManifest Load() {
        if (!File.Exists(_manifestPath)) {
            return new ModpackManifest();
        }

        try {
            var json = File.ReadAllText(_manifestPath);
            ModpackManifest modpackManifest = JsonSerializer.Deserialize<ModpackManifest>(json) ?? new ModpackManifest();
            Manifest = modpackManifest;
            return modpackManifest;
        }
        catch {
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
        // Ne asigurăm că folderul există înainte de scriere
        var directory = Path.GetDirectoryName(_manifestPath);
        if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);

        var json = JsonSerializer.Serialize(Manifest, AppVariables.DefaultJsonOptions);
        File.WriteAllText(_manifestPath, json);
    }

    public void ParseAllMods(Action<InstalledModInfo> action) {
        foreach (InstalledModInfo modInfo in Manifest.InstalledMods) {
            action.Invoke(modInfo);
        }
        Save();
    }



    public bool AddOrUpdateMod(InstalledModInfo modInfo, bool modpackInstall) {
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
        if (modpackInstall) installedModInfo.Source = ModSource.Remote;
        Manifest.InstalledMods.Add(installedModInfo);
        Save();
    }

    public InstalledModInfo? AddMod(ModrinthProject project, ModrinthVersion version) {
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
            Filename = file.Filename,
            DownloadUrl = file.Url,
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
        var mod = Manifest.InstalledMods
            .FirstOrDefault(m => m.ProjectId == modInfo.ProjectId);

        if (mod == null)
            return ModInstallState.NotInstalled;

        if (mod.VersionId == modInfo.VersionId)
            return ModInstallState.InstalledSameVersion;

        return ModInstallState.InstalledDifferentVersion;
    }

    public async Task<bool> DownloadModAsync(InstalledModInfo modInfo, string installPath) {
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
}


public enum ModInstallState {
    NotInstalled,
    InstalledSameVersion,
    InstalledDifferentVersion
}