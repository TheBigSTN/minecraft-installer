using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading.Tasks;
using ModpackInstaller.Models;
using ModpackInstaller.Services.Helpers;

namespace ModpackInstaller.Services.Modpack;

public sealed class ModpackManifestStorage : StoredData<ModpackManifest> {
    protected override string FilePath => Path.Combine(InstallPath, "manifest.json");
    
    public readonly string InstallPath;
    
    public ObservableList<ModInfo> InstalledMods { get; } = [];

    private ModpackManifestStorage(string modpackInstallPath) {
        _installManager = new Lazy<ModInstallationManager>(() => new ModInstallationManager(this));
        _filesystemService = new Lazy<ModFilesystemSyncService>(() => new ModFilesystemSyncService(this, InstallationManager));
        _stateService = new Lazy<ModStateService>(() => new ModStateService(this));
        InstallPath = modpackInstallPath;
    }
    
    private static readonly ConcurrentDictionary<string, ModpackManifestStorage> Instances = new();

    public static async Task<ModpackManifestStorage> CreateInstanceAsync(string modpackInstallPath) {
        modpackInstallPath = Path.GetFullPath(modpackInstallPath);
        
        var store = Instances.GetOrAdd(modpackInstallPath, path => new ModpackManifestStorage(path));
        await store.LoadAsync().ConfigureAwait(false);
        
        return store;
    }
    
    public static bool ReleaseInstance(ModpackManifestStorage store) {
        return Instances.TryRemove(Path.GetFullPath(store.InstallPath), out _);
    }
    
    public static bool ReleaseInstance(string installPath) {
        return Instances.TryRemove(installPath, out _);
    }

    protected override void OnLoaded() {
        InstalledMods.Clear();
        InstalledMods.AddRange(Data.InstalledMods);
    }

    protected override void OnSaved() {
        // Sync back to the base Data object just in case items were added/removed outside serialization bounds
        Data.InstalledMods = [.. InstalledMods];
    }

    public ModpackManifest GetManifest() => Data;
    
    private readonly Lazy<ModInstallationManager> _installManager;
    private readonly Lazy<ModFilesystemSyncService> _filesystemService;
    private readonly Lazy<ModStateService> _stateService;

    public ModInstallationManager InstallationManager => _installManager.Value;
    public ModFilesystemSyncService FilesystemSyncService => _filesystemService.Value;
    public ModStateService StateService => _stateService.Value;

}