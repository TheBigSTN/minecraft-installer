using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using ModpackInstaller.Infrastructure;

namespace ModpackInstaller.Services.Helpers;

public abstract class StoredData<T> where T : class, IMigratedData, new() {
    protected abstract string FilePath { get; }

    protected T Data { get; set; } = new();
    private bool IsDirty { get; set; }
    public DateTime LastLoaded { get; private set; }
    public DateTime LastSaved { get; private set; }

    public void MarkDirty() => IsDirty = true;
    
    private readonly SemaphoreSlim _ioLock = new(1, 1);

    public async Task LoadAsync() {
        await _ioLock.WaitAsync().ConfigureAwait(false);
        
        try {
            var json = string.Empty;
            if (File.Exists(FilePath)) {
                json = await File.ReadAllTextAsync(FilePath).ConfigureAwait(false);
            }

            var root = string.IsNullOrWhiteSpace(json)
                ? new JsonObject()
                : JsonNode.Parse(json)?.AsObject() ?? new JsonObject();

            var fileVersion = root["_version"]?.GetValue<int>() ?? 0;

            if (fileVersion < T.SchemaVersion) {
                T.Migrate(root, fileVersion);
                MarkDirty();
            }

            Data = root.Deserialize<T>(AppVariables.WebJsonOptions) ?? new T();
            LastLoaded = DateTime.UtcNow;
            OnLoaded();
        }
        finally {
            _ioLock.Release();
        }
    }

    public async Task SaveAsync() {
        if (!IsDirty) return;

        await _ioLock.WaitAsync();

        try {
            var root = JsonSerializer.SerializeToNode(Data)?.AsObject() ?? new JsonObject();
            root["_version"] = T.SchemaVersion;

            var json = root.ToJsonString(new JsonSerializerOptions { WriteIndented = true });

            var dir = Path.GetDirectoryName(FilePath);
            if (!string.IsNullOrEmpty(dir)) {
                Directory.CreateDirectory(dir);
            }

            await File.WriteAllTextAsync(FilePath, json);

            IsDirty = false;
            LastSaved = DateTime.UtcNow;
            OnSaved();
            OnSave?.Invoke();
        }
        finally {
            _ioLock.Release();
        }
    }

    public Task ReloadAsync() => LoadAsync();

    protected virtual void OnLoaded() { }

    protected virtual void OnSaved() { }

    public event Action? OnSave;
}