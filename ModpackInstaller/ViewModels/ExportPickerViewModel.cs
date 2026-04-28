using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Text;
using System.Threading.Tasks;
using ModpackInstaller.Models;
using ModpackInstaller.Services.Modpack;
using ReactiveUI;

namespace ModpackInstaller.ViewModels;
public class ExportPickerViewModel : ViewModelBase {
    public FileNode Root { get; }

    public ReactiveCommand<Unit, Unit> ConfirmCommand { get; }

    public event Action<List<string>>? CloseRequested;

    private readonly HashSet<string> _excludedRelativePaths;

    public ExportPickerViewModel(string modpackPath) {
        _excludedRelativePaths = [];

        var manifestService = new ModpackManifestService(modpackPath);
        manifestService.Load();

        foreach(var mod in manifestService.Manifest.InstalledMods) {
            if(mod.Source == ModSource.Local)
                continue;
            // ex: mods/sodium.jar
            var relative = Path.Combine("mods", mod.Filename);

            _excludedRelativePaths.Add(relative);
        }

        //These are files that for compatability are exluded by default;
        _excludedRelativePaths.Add("TLauncherAdditional.json");
        _excludedRelativePaths.Add("usercache.json");
        _excludedRelativePaths.Add("usernamecache.json");
        _excludedRelativePaths.Add("command_history.txt");
        _excludedRelativePaths.Add("hs_err_pid*");
        _excludedRelativePaths.Add("win_event*");

        // This is compat for a password mod, you don't want to share your password when sharing a modpack
        _excludedRelativePaths.Add(".sl_password");

        // These are files from other mods that you would not want
        _excludedRelativePaths.Add("emi.json");
        _excludedRelativePaths.Add("xaero");

        Root = BuildTree(modpackPath, "");

        ConfirmCommand = ReactiveCommand.Create(Confirm);
    }

    // 🔧 build tree + aplicare excluderi
    private FileNode BuildTree(string basePath, string path ) {
        var node = new FileNode(basePath, path);

        if(path == "manifest.json") {
            node.IsChecked = true;
            node.Lock();
        }

        if(path == "mods")
            node.Lock();
            

        if(node.IsDirectory) {
            foreach(var dir in Directory.GetDirectories(node.FullPath))
                node.Children.Add(BuildTree(basePath, Path.GetRelativePath(basePath, dir)));

            foreach(var file in Directory.GetFiles(node.FullPath))
                node.Children.Add(BuildTree(basePath, Path.GetRelativePath(basePath, file)));
        }
        
        if(_excludedRelativePaths.Any(pattern => Match(pattern, path))) {
            node.IsChecked = false;
        }

        return node;
    }

    // 🔧 colectare excluded
    private void Confirm() {
        var excluded = new List<string>();
        CollectExcluded(Root, excluded);

        CloseRequested?.Invoke(excluded);
    }

    private static void CollectExcluded( FileNode node, List<string> excluded ) {
        if(!node.IsChecked && !node.IsDirectory) {
            excluded.Add(node.FullPath);
            return;
        }

        foreach(var child in node.Children)
            CollectExcluded(child, excluded);
    }

    private static bool Match( string pattern, string path ) {
        // normalizare minimă
        pattern = pattern.Replace('\\', '/');
        path = path.Replace('\\', '/');

        // fără wildcard → exact match
        if(!pattern.Contains('*'))
            return string.Equals(pattern, path, StringComparison.OrdinalIgnoreCase);

        // wildcard simplu: transformăm în regex
        var regex = "^" + System.Text.RegularExpressions.Regex
            .Escape(pattern)
            .Replace("\\*", ".*") + "$";

        return System.Text.RegularExpressions.Regex
            .IsMatch(path, regex, System.Text.RegularExpressions.RegexOptions.IgnoreCase);
    }
}