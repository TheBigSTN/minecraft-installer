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
using DynamicData;
using ModpackInstaller.Infrastructure;
using ModpackInstaller.Models;

public class ModpackPackageService() {

    // =========================
    // EXPORT MODPACK (full package)
    // =========================
    public static string ExportFullAsync(string modpackInstallPath, string outputZipPath, IEnumerable<string> excludedPaths ) {
        var tempFolderPath = CreateTempFolder();

        try {
            if (File.Exists(outputZipPath))
                File.Delete(outputZipPath);

            //AddToExport("manifest.json", modpackInstallPath, tempFolderPath);

            //var manifest = ModpackManifestService.Load(Path.Combine(tempFolderPath, "manifest.json"));

            //AddToExport("mods", modpackInstallPath, tempFolderPath, fileName =>
            //    !manifest.InstalledMods.Exists(m => m.Filename.Equals(fileName, StringComparison.OrdinalIgnoreCase)));

            //AddToExport("config", modpackInstallPath, tempFolderPath);
            //AddToExport("resourcepacks", modpackInstallPath, tempFolderPath);


            AddToExport(modpackInstallPath, modpackInstallPath, tempFolderPath, excludedPaths.ToList());


            Directory.CreateDirectory(Path.Combine(outputZipPath, ".."));

            ZipFile.CreateFromDirectory(tempFolderPath, outputZipPath);

            return outputZipPath;
        }
        finally {
            Directory.Delete(tempFolderPath, true);
        }
    }

    // =========================
    // IMPORT MODPACK
    // =========================
    //public static async Task ImportAsync(string zipPath, string installPath, ModpackMetadata metadata) {
    //    var tempFolder = CreateTempFolder();

    //    try {
    //        ZipFile.ExtractToDirectory(zipPath, tempFolder);

    //        var metadataPath = Path.Combine(installPath, "metadata.json");
    //        await SaveJsonAsync(metadataPath, metadata);

    //        foreach (var folder in new[] { "mods", "config", "resourcepacks", "serverdata", "overrides" }) {
    //            AddToExport(folder, tempFolder, installPath);
    //        }
    //    }
    //    finally {
    //        Directory.Delete(tempFolder, true);
    //    }
    //}

    // =========================
    // HELPER: AddToExport
    // =========================
    //private static void AddToExport(string relativeFilePath, string sourceDirectory, string outputFolder, Func<string, bool>? filter = null) {
    //    var sourcePath = Path.Combine(sourceDirectory, relativeFilePath);
    //    if (string.IsNullOrEmpty(relativeFilePath) || (!Directory.Exists(sourcePath) && !File.Exists(sourcePath)))
    //        return;

    //    if (File.Exists(sourcePath)) {
    //        var fileName = Path.GetFileName(sourcePath);
    //        if (filter == null || filter(fileName)) {
    //            Directory.CreateDirectory(outputFolder);
    //            File.Copy(sourcePath, Path.Combine(outputFolder, fileName), true);
    //        }
    //        return;
    //    }

    //    var destDir = Path.Combine(outputFolder, relativeFilePath);
    //    Directory.CreateDirectory(destDir);

    //    foreach (var file in Directory.GetFiles(sourcePath)) {
    //        var fileName = Path.GetFileName(file);
    //        if (filter == null || filter(fileName))
    //            File.Copy(file, Path.Combine(destDir, fileName), true);
    //    }

    //    foreach (var subDir in Directory.GetDirectories(sourcePath)) {
    //        AddToExport(Path.GetFileName(subDir), sourcePath, destDir, filter);
    //    }
    //}

    private static void AddToExport(string sourcePath, string sourceBasePath, string outputPath, List<string> exludedFipePaths) {
        if (string.IsNullOrEmpty(sourcePath))
            return;

        if(Directory.Exists(sourcePath)) {
            foreach(var DirectoryPath in Directory.GetDirectories(sourcePath))
                AddToExport(DirectoryPath, sourceBasePath, outputPath, exludedFipePaths);

            foreach(var DirectoryPath in Directory.GetFiles(sourcePath))
                AddToExport(DirectoryPath, sourceBasePath, outputPath, exludedFipePaths);
        }
        if(File.Exists(sourcePath)) {
            if (!exludedFipePaths.Contains(sourcePath)) {
                Directory.CreateDirectory(Path.Combine(outputPath, Path.GetDirectoryName(Path.GetRelativePath(sourceBasePath, sourcePath))!));
                File.Copy(sourcePath, Path.Combine(outputPath, Path.GetRelativePath(sourceBasePath, sourcePath)));
            }
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
