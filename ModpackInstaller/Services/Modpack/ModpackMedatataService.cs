using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using ModpackInstaller.Infrastructure;
using ModpackInstaller.Models;

namespace ModpackInstaller.Services.Modpack;

public class ModpackMedatataService {
    private readonly string _registryPath;
    public ModpackMedatataService(string installerRoot) {
        _registryPath = Path.Combine(installerRoot, "modpacks");
        Directory.CreateDirectory(_registryPath);
    }

    /// <summary>
    /// Uses default AppVariables.InstallerRoot
    /// </summary>
    public ModpackMedatataService() {
        _registryPath = Path.Combine(AppVariables.InstallerRoot, "modpacks");
        Directory.CreateDirectory(_registryPath);
    }

    // 📌 Creează / înregistrează metadata
    public ModpackMetadata Create(ModpackMetadata metadata) {
        metadata.CreatedAt = DateTime.UtcNow;
        metadata.UpdatedAt = metadata.CreatedAt;

        Save(metadata);
        return metadata;
    }

    // 📌 Salvează metadata extern
    public void Save(ModpackMetadata metadata) {
        metadata.UpdatedAt = DateTime.UtcNow;

        var path = GetMetadataPath(metadata.Id);
        var json = JsonSerializer.Serialize(metadata, AppVariables.DefaultJsonOptions);
        File.WriteAllText(path, json);
    }

    // 📌 Încearcă să încarce metadata (fără erori)
    public bool TryLoad(string id, out ModpackMetadata? metadata) {
        metadata = null;
        var path = GetMetadataPath(id);

        if (!File.Exists(path))
            return false;

        try {
            metadata = JsonSerializer.Deserialize<ModpackMetadata>(
                File.ReadAllText(path)
            );
            return metadata != null;
        }
        catch {
            return false;
        }
    }

    // 📌 Load direct (poate întoarce null)
    public ModpackMetadata? Load(string id) {
        TryLoad(id, out var metadata);
        return metadata;
    }

    // 📌 Există în registry
    public bool Exists(string id)
        => File.Exists(GetMetadataPath(id));

    // 📌 Update parțial, safe
    public bool Update(string id, Action<ModpackMetadata> update) {
        if (!TryLoad(id, out var metadata) || metadata == null)
            return false;

        update(metadata);
        Save(metadata);
        return true;
    }

    // 📌 Șterge metadata
    public bool Delete(string id) {
        var path = GetMetadataPath(id);
        if (!File.Exists(path))
            return false;

        File.Delete(path);
        return true;
    }

    // 📌 Enumeră TOATE modpack-urile din registry
    public IEnumerable<ModpackMetadata> LoadAll() {
        foreach (var file in Directory.GetFiles(_registryPath, "*.json")) {
            ModpackMetadata? metadata = null;
            try {
                metadata = JsonSerializer.Deserialize<ModpackMetadata>(
                    File.ReadAllText(file)
                );
            }
            catch { }

            if (metadata != null)
                yield return metadata;
        }
    }

    private string GetMetadataPath(string id)
        => Path.Combine(_registryPath, $"{id}.json");
}