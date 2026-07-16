using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text.Json;
using ModpackInstaller.Infrastructure;
using ModpackInstaller.Models;
using ModpackInstaller.Models.DTOs;

namespace ModpackInstaller.Services.Modpack;

public class ModpackTreeService {
    private readonly string _modpackInstallPath;
    private readonly string _treeJsonPath;

    public ModpackTreeDto ModpackTree;

    public ModpackTreeService(string modpackInstallPath) {
        _modpackInstallPath = modpackInstallPath;
        _treeJsonPath = GetTreeJsonPath(_modpackInstallPath);
        
        Load();
    }

    public void Save() {
        Save(_treeJsonPath, ModpackTree);
    }
    
    public static void Save(string treeJsonPath, ModpackTreeDto tree) {
        var directory = Path.GetDirectoryName(treeJsonPath);
        if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);

        var json = JsonSerializer.Serialize(tree, AppVariables.DefaultJsonOptions);
        
        File.WriteAllText(treeJsonPath, json);
    }
    
    [MemberNotNull(nameof(ModpackTree))]
    public void Load() {
        if (!File.Exists(_treeJsonPath)) {
            ModpackTree = new ModpackTreeDto([]);
            return;
        }

        try {
            var json = File.ReadAllText(_treeJsonPath);
            ModpackTree = JsonSerializer.Deserialize<ModpackTreeDto>(json) ?? new ModpackTreeDto([]);
        }catch {
            ModpackTree = new ModpackTreeDto([]);
        }
    }

    public static string GetTreeJsonPath(string basePath) {
        return Path.Combine(basePath, "tree.json");
    }
}