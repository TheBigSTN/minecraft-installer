using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModpackInstaller.Services.Modpack;

using System;
using System.IO;
using System.IO.Compression;
using System.Text.Json;
using System.Threading.Tasks;
using Avalonia.Controls;
using ModpackInstaller.Infrastructure;
using ModpackInstaller.Models;

public class ModpackPackageService() {

    // =========================
    // EXPORT MODPACK (full package)
    // =========================
    public static string ExportFullAsync(string installPath, string outputZipPath) {
        var tempFolder = CreateTempFolder();

        try {
            // 1️⃣ Copiem manifest.json primul
            AddToExport("manifest.json", installPath, tempFolder);

            // 2️⃣ Încarci manifest pentru verificarea modurilor
            // TODO: adaugă aici logica ta de încărcare manifest
            var manifest = ModpackManifestService.Load(Path.Combine(tempFolder, "manifest.json"));

            // 3️⃣ Copiem mods filtrat (exclude modurile deja în manifest)
            AddToExport("mods", installPath, tempFolder, fileName =>
                !manifest.InstalledMods.Exists(m => m.Filename.Equals(fileName, StringComparison.OrdinalIgnoreCase)));

            // 4️⃣ Copiem restul folderelor relevante
            AddToExport("config", installPath, tempFolder);
            AddToExport("resourcepacks", installPath, tempFolder);

            Directory.CreateDirectory(Path.Combine(outputZipPath, ".."));

            // 5️⃣ Creează ZIP-ul final
            ZipFile.CreateFromDirectory(tempFolder, outputZipPath);

            return outputZipPath;
        }
        finally {
            Directory.Delete(tempFolder, true);
        }
    }

    // =========================
    // IMPORT MODPACK
    // =========================
    public async Task ImportAsync(string zipPath, string installPath, ModpackMetadata metadata) {
        var tempFolder = CreateTempFolder();

        try {
            // 1️⃣ Extragem ZIP-ul temporar
            ZipFile.ExtractToDirectory(zipPath, tempFolder);

            // 2️⃣ Copiem metadata la locul potrivit
            // TODO: adaugă aici logica ta pentru salvarea metadata
            var metadataPath = Path.Combine(installPath, "metadata.json");
            await SaveJsonAsync(metadataPath, metadata);

            // 3️⃣ Copiem folderele standard în folderul de instalare
            foreach (var folder in new[] { "mods", "config", "resourcepacks", "serverdata", "overrides" }) {
                AddToExport(folder, tempFolder, installPath);
            }

            // 4️⃣ TODO: reconciliere manifest / rebuild index
        }
        finally {
            Directory.Delete(tempFolder, true);
        }
    }

    // =========================
    // HELPER: AddToExport
    // =========================
    /// <summary>
    /// Copiază un fișier sau folder din installPath în targetFolder.
    /// Acceptă optional filter pentru fișiere.
    /// </summary>
    private static void AddToExport(string name, string installPath, string targetFolder, Func<string, bool>? filter = null) {
        var sourcePath = Path.Combine(installPath, name);
        if (string.IsNullOrEmpty(name) || (!Directory.Exists(sourcePath) && !File.Exists(sourcePath)))
            return;

        if (File.Exists(sourcePath)) {
            var fileName = Path.GetFileName(sourcePath);
            if (filter == null || filter(fileName)) {
                Directory.CreateDirectory(targetFolder);
                File.Copy(sourcePath, Path.Combine(targetFolder, fileName), true);
            }
            return;
        }

        // sourcePath este un folder
        var destDir = Path.Combine(targetFolder, name);
        Directory.CreateDirectory(destDir);

        foreach (var file in Directory.GetFiles(sourcePath)) {
            var fileName = Path.GetFileName(file);
            if (filter == null || filter(fileName))
                File.Copy(file, Path.Combine(destDir, fileName), true);
        }

        foreach (var subDir in Directory.GetDirectories(sourcePath)) {
            AddToExport(Path.GetFileName(subDir), sourcePath, destDir, filter);
        }
    }

    // =========================
    // HELPERS GENERALE
    // =========================
    private static string CreateTempFolder() {
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(path);
        return path;
    }

    private static async Task SaveJsonAsync<T>(string path, T obj) {
        var json = JsonSerializer.Serialize(obj, AppVariables.WebJsonOptions);
        await File.WriteAllTextAsync(path, json);
    }
}
