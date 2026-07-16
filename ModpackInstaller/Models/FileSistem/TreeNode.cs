using System.Collections.ObjectModel;
using ReactiveUI;
using ReactiveUI.SourceGenerators;

namespace ModpackInstaller.Models.FileSistem;
#pragma warning disable RXUISG0016
public partial class TreeNode : ReactiveObject {

    public required TreeNodeInfo Node { get; init; }

    public ObservableCollection<TreeNode> Children { get; } = [];

    public string Name => Node.Name;

    public bool IsDirectory => Node.IsDirectory;
    
    public bool IsFile => !Node.IsDirectory;

    public FileSelectionState SelectionState { get; init; }

    public bool CanChange => SelectionState == FileSelectionState.Normal;

    [Reactive] 
    private bool _isExpanded;

    public string? Description =>
        SelectionState switch {
            FileSelectionState.Managed =>
                "Managed by Modpack Installer",

            FileSelectionState.Required =>
                "Required by Modpack Installer",
            
            FileSelectionState.Downloadable =>
                "Downloadable content",

            _ => null
        };

    private bool _isChecked = true;

    public bool IsChecked {
        get => _isChecked;
        set {
            if (!CanChange)
                return;

            this.RaiseAndSetIfChanged(ref _isChecked, value);

            if (!IsDirectory) return;
            foreach (var child in Children)
                child.IsChecked = value;
        }
    }
}
#pragma warning restore RXUISG0016