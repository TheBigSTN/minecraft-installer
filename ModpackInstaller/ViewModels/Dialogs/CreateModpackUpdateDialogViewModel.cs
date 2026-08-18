using System;
using System.IO;
using System.Linq;
using System.Reactive;
using ModpackInstaller.Models;
using ModpackInstaller.Models.FileSistem;
using ModpackInstaller.Services.FileSistem;
using ModpackInstaller.Services.Modpack;
using ReactiveUI;
using ReactiveUI.SourceGenerators;

namespace ModpackInstaller.ViewModels.Dialogs;

public partial class CreateModpackUpdateDialogViewModel : DialogViewModel<bool> {

    [Reactive] private string? _versionName;
    [Reactive] private string? _semver;
    
    public ReactiveCommand<Unit, Unit> CreateCommand { get; }
    public ReactiveCommand<Unit, Unit> CloseCommand { get; }
    
    public Func<bool> ValidateUpdatePage => ValidateUpdate;
    public TreeNode Root { get; }

    private readonly FileSelectionRules _rules;

    public CreateModpackUpdateDialogViewModel(ModpackMetadataStorage metadataStorage,
                                              ModpackManifestStorage manifestStorage) {
        var modpackPath = metadataStorage.GetData().InstallPath;
        _rules = new FileSelectionRules(manifestStorage);

        Root = BuildTree(modpackPath, "");
        Root.IsExpanded = true;

        CreateCommand = ReactiveCommand.CreateFromTask(async () => {
            if (string.IsNullOrEmpty(VersionName) ||
                string.IsNullOrEmpty(Semver)) {
                // The error is shown on the ui via the validator
                return;
            }
            
            ModpackPublicizeService service = new(metadataStorage);

            await service.UploadNewVersionAsync(Root, Semver, VersionName);

            metadataStorage.Update(local => {
                local.VersionSemver = Semver;
            });
            
            Close(true);
        });

        CloseCommand = ReactiveCommand.Create(() => {
            Close(false);
        });
    }

    private TreeNode BuildTree(string basePath, string relativePath) {
        var fullPath = Path.Combine(basePath, relativePath);

        var fileInfo = new TreeNodeInfo {
            FullPath = fullPath,
            RelativePath = relativePath,
            Name = string.IsNullOrEmpty(relativePath) ? "Root" : Path.GetFileName(relativePath),
            IsDirectory = Directory.Exists(fullPath),
        };

        var node = new TreeNode {
            Node = fileInfo,
            IsChecked = !_rules.IsDisabledByDefault(relativePath),
            SelectionState = _rules.GetState(relativePath)
        };

        if (node.IsFile) return node;
        
        foreach (var childInfo in FileSystemService.GetDirectoryContents(fullPath, basePath)) {
            node.Children.Add(BuildTree(basePath, childInfo.RelativePath));
        }

        return node;
    }
    
    [Reactive]
    private string? _validationError;

    private bool ValidateUpdate() {
        ValidationError = string.Empty;

        if (string.IsNullOrWhiteSpace(VersionName)) {
            ValidationError = "Version name is required.";
            return false;
        }

        // ReSharper disable once InvertIf
        if (string.IsNullOrWhiteSpace(Semver)) {
            ValidationError = "Version number is required.";
            return false;
        }

        return true;
    }
}