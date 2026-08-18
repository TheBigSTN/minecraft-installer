using System;
using System.IO;
using System.Linq;
using ModpackInstaller.Models;

namespace ModpackInstaller.Services.Modpack;

public class ModStateService(ModpackManifestStorage store) {
    private readonly string _installPath = store.InstallPath;

    public ObservableList<ModInfo> InstalledMods => store.InstalledMods;

    public void EnableDisableMod(string projectId, bool status) {
        var mod = InstalledMods.FirstOrDefault(m => m.ProjectId == projectId);
        if (mod == null || mod.Enabled == status) return;
        
        var filePath = Path.Combine(_installPath, "mods", mod.Filename);
        var deactivationPath = filePath + ".deactivation";

        try {
            if (status && File.Exists(deactivationPath)) {
                File.Move(deactivationPath, filePath, true);
            } else if (!status && File.Exists(filePath)) {
                File.Move(filePath, deactivationPath, true);
            }
        }
        catch (Exception ex) {
            Console.WriteLine($"Failed to toggle mod file state: {ex.Message}");
        }
        
        mod.Enabled = status;
        store.MarkDirty();
    }
    
    public bool IsModInstalled(IModVersion modInfo) => 
        GetModInstallState(modInfo) is ModInstallState.InstalledSameVersion or ModInstallState.InstalledDifferentVersion;

    public ModInstallState GetModInstallState(IModVersion modInfo) {
        var mod = InstalledMods.FirstOrDefault(m => m.ProjectId == modInfo.ProjectId);
        if (mod == null) return ModInstallState.NotInstalled;

        return mod.VersionId == modInfo.VersionId
            ? ModInstallState.InstalledSameVersion
            : ModInstallState.InstalledDifferentVersion;
    }
}