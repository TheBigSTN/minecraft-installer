using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Threading.Tasks;
using Avalonia.Threading;
using ReactiveUI;

namespace ModpackInstaller.ViewModels.ModpackBody;

public class FilesViewModel : ViewModelBase {
    private readonly string _rootPath;
    private string _currentPath;

    public string CurrentPath {
        get => _currentPath;
        private set => this.RaiseAndSetIfChanged(ref _currentPath, value);
    }

    private string _currentFolderName;

    public string CurrentFolderName {
        get => _currentFolderName;
        private set => this.RaiseAndSetIfChanged(ref _currentFolderName, value);
    }

    public bool IsRoot => CurrentPath.Equals(_rootPath, StringComparison.OrdinalIgnoreCase);

    public ObservableCollection<FileItem> Files { get; } = [];

    public ReactiveCommand<FileItem, Unit> NavigateCommand { get; }
    public ReactiveCommand<Unit, Unit> GoHomeCommand { get; }
    public ReactiveCommand<Unit, Unit> RefreshCommand { get; }

    public FilesViewModel(string instancePath) {
        _rootPath = Path.GetFullPath(instancePath);
        _currentPath = _rootPath;
        _currentFolderName = Path.GetFileName(_rootPath);

        NavigateCommand = ReactiveCommand.Create<FileItem>(item => {
            if (item.IsDirectory) {
                LoadDirectory(item.FullPath);
            }
        });

        GoHomeCommand = ReactiveCommand.Create(() => { LoadDirectory(_rootPath); });

        RefreshCommand = ReactiveCommand.Create(() => { LoadDirectory(CurrentPath); });

        LoadDirectory(_rootPath);
    }

    public FilesViewModel() : this(@"C:\Users\SARA\curseforge\minecraft\Instances\Test\afsdaeff") {}

    private void LoadDirectory(string path) {
        CurrentPath = path;
        CurrentFolderName = Path.GetFileName(path);
        this.RaisePropertyChanged(nameof(IsRoot));

        Files.Clear();
        try {
            var entries = Directory.EnumerateFileSystemEntries(path)
                                   .OrderByDescending(Directory.Exists)
                                   .ThenBy(Path.GetFileName, StringComparer.OrdinalIgnoreCase);

            foreach (var entry in entries) {
                Files.Add(new FileItem(entry));
            }
        }
        catch (Exception ex) {
            // Handle unauthorized access or IO errors if necessary
        }
    }
}