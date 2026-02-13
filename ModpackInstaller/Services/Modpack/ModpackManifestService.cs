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

    public ModpackManifestService(string installPath) {
        _installPath = installPath;
        _manifestPath = Path.Combine(installPath, "manifest.json");
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

    public void Save() {
        // Ne asigurăm că folderul există înainte de scriere
        var directory = Path.GetDirectoryName(_manifestPath);
        if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);

        var json = JsonSerializer.Serialize(Manifest, AppVariables.DefaultJsonOptions);
        File.WriteAllText(_manifestPath, json);
    }

    public InstalledModInfo AddMod(ModrinthProject project, ModrinthVersion version) {
        // Verificăm dacă există deja în lista de tip InstalledModInfo
        if (Manifest.InstalledMods.Any(m => m.ProjectId == project.Id))
            return null;

        var file = version.PrimaryFile;
        if (file == null) return null;

        // AICI: Creăm obiectul nou pentru listă
        var newItem = new InstalledModInfo {
            ProjectId = project.Id,
            VersionId = version.Id,
            Title = project.Title,
            Filename = file.Filename,
            DownloadUrl = file.Url,
            IconUrl = project.IconURL // Verifică dacă e IconURL sau IconUrl în clasa ta
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

    public async Task DownloadModAsync(InstalledModInfo modInfo, string installPath) {
        try {
            var modsFolder = Path.Combine(installPath, "mods");
            if (!Directory.Exists(modsFolder)) Directory.CreateDirectory(modsFolder);

            var filePath = Path.Combine(modsFolder, modInfo.Filename);

            // Folosim un HttpClient simplu (poți refolosi instanța din WebService dacă e publică)
            using var client = new HttpClient();
            var data = await client.GetByteArrayAsync(modInfo.DownloadUrl);
            await File.WriteAllBytesAsync(filePath, data);

            Debug.WriteLine($"[Download] Finalizat: {modInfo.Title}");
        }
        catch (Exception ex) {
            Debug.WriteLine($"[Error] Descărcare eșuată pentru {modInfo.Title}: {ex.Message}");
        }
    }


}