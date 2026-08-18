using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using ModpackInstaller.Models;
using ModpackInstaller.Models.Interfaces;
using ModpackInstaller.Services.Helpers;

namespace ModpackInstaller.Services.Modpack;

public sealed class ModpackMetadataStorage : StoredData<ModpackMetadata> {
    protected override string FilePath { get; }
    
    protected override void OnSaved() {
        Data.UpdatedAt = DateTimeOffset.UtcNow;
    }

    // Initialize with a specific file path
    public ModpackMetadataStorage(string filePath) {
        FilePath = filePath;
    }

    public ModpackMetadataStorage(string filePath, ModpackMetadata metadata) {
        FilePath = filePath;
        Data = metadata;
        MarkDirty();
    }

    public void InitializeNew(Guid modpackId, string name, string? installPath = null) {
        Data.Id = Guid.NewGuid();
        Data.ModpackId = modpackId;
        Data.Name = name;
        Data.InstallPath = installPath;
        Data.CreatedAt = DateTime.UtcNow;
        Data.UpdatedAt = Data.CreatedAt;
        MarkDirty();
    }

    public void UpdateName(string name) {
        Data.Name = name;
        Data.UpdatedAt = DateTime.UtcNow;
        MarkDirty();
    }

    public void UpdateInstallPath(string? installPath) {
        Data.InstallPath = installPath;
        Data.UpdatedAt = DateTime.UtcNow;
        MarkDirty();
    }

    /// <summary>
    /// Returns a deep copy (or a read-only snapshot) of the metadata 
    /// so external callers cannot mutate internal state directly.
    /// </summary>
    public IReadOnlyModpackMetadata GetData() => Data;

    /// <summary>
    /// Controlled mutation method. Executes your action and automatically marks the storage as dirty.
    /// </summary>
    public void Update(Action<ModpackMetadata> mutationAction) {
        mutationAction(Data);
        Data.UpdatedAt = DateTime.UtcNow; // Automatically keep timestamp fresh if desired
        MarkDirty();
    }
    
    /// <summary>
    /// Checks whether the modpack has been published based on its metadata state.
    /// </summary>
    public bool IsPublished => Data.ModpackId is not null && !string.IsNullOrEmpty(Data.ModpackPassword);
    
    public bool Exists => File.Exists(FilePath);
}