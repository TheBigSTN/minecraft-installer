using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using ModpackInstaller.Infrastructure;
using ModpackInstaller.Models;

namespace ModpackInstaller.Services.Modpack;

public static class ModpackMetadataRegistry {
    private static readonly string DefaultRegistryPath = Path.Combine(AppVariables.InstallerRoot, "modpacks");

    public static event Action? MetadataChanged;

    static ModpackMetadataRegistry() {
        Directory.CreateDirectory(DefaultRegistryPath);
    }

    public static string GetPath(Guid id) => Path.Combine(DefaultRegistryPath, $"{id:N}.json");

    public static bool Exists(Guid id) => File.Exists(GetPath(id));

    public static async Task<ModpackMetadataStorage?> LoadAsync(Guid id) {
        var path = GetPath(id);
        if (!File.Exists(path)) return null;

        var storage = new ModpackMetadataStorage(path);
        
        storage.OnSave += () => MetadataChanged?.Invoke();
        
        await storage.LoadAsync().ConfigureAwait(false);
        return storage;
    }

    public static async Task CreateAsync(ModpackMetadata metadata) {
        var storage = new ModpackMetadataStorage(GetPath(metadata.Id), metadata);
        await storage.SaveAsync().ConfigureAwait(false);

        MetadataChanged?.Invoke();
    }

    public static IReadOnlyList<ModpackMetadata> LoadAll() {
        if (!Directory.Exists(DefaultRegistryPath))
            return [];

        var results = new List<ModpackMetadata>();

        foreach (var file in Directory.GetFiles(DefaultRegistryPath, "*.json")) {
            try {
                var json = File.ReadAllText(file);
                var metadata = JsonSerializer.Deserialize<ModpackMetadata>(json, AppVariables.DefaultJsonOptions);

                if (metadata != null) {
                    results.Add(metadata);
                }
            }
            catch {
                // Skip corrupted files safely
            }
        }

        return results;
    }

    public static bool Delete(Guid id, out DeleteError failReason) {
        failReason = DeleteError.None;
        var path = GetPath(id);

        if (!File.Exists(path)) {
            failReason = DeleteError.MetadataNotFound;
            return false;
        }

        try {
            var json = File.ReadAllText(path);
            var metadata = JsonSerializer.Deserialize<ModpackMetadata>(json, AppVariables.DefaultJsonOptions);

            if (!string.IsNullOrWhiteSpace(metadata?.InstallPath) && Directory.Exists(metadata.InstallPath)) {
                Directory.Delete(metadata.InstallPath, true);
            }
        }
        catch {
            failReason = DeleteError.DirectoryDeleteFailed;
            return false;
        }

        try {
            File.Delete(path);
            MetadataChanged?.Invoke();
        }
        catch {
            failReason = DeleteError.MetadataDeleteFailed;
            return false;
        }

        return true;
    }

    public enum DeleteError {
        None,
        MetadataNotFound,
        DirectoryDeleteFailed,
        MetadataDeleteFailed
    }
}