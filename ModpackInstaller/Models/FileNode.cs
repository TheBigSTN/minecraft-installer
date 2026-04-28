using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ReactiveUI;

namespace ModpackInstaller.Models;
public class FileNode : ReactiveObject {
    public string Name { get; }
    public string FullPath { get; }
    public string RelativePath { get; }
    public bool IsDirectory { get; }
    public bool IsLocked { get; private set; }

    public ObservableCollection<FileNode> Children { get; } = new();

    private bool _isChecked = true;
    public bool IsChecked {
        get => _isChecked;
        set {
            if(IsLocked)
                return;

            this.RaiseAndSetIfChanged(ref _isChecked, value);

            if(IsDirectory) {
                foreach(var child in Children)
                    child.IsChecked = value;
            }
        }
    }

    public FileNode(string basePath, string path ) {
        FullPath = Path.Combine(basePath, path);
        RelativePath = path;
        Name = Path.GetFileName(path);
        IsDirectory = Directory.Exists(FullPath);
        IsLocked = false;
    }

    public void Lock() {
        IsLocked = true;
    }
}