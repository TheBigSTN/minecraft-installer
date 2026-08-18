using System;
using System.IO;
using System.Linq;
using ReactiveUI;

namespace ModpackInstaller.ViewModels.ModpackBody;

public class FileItem : ReactiveObject {
    public string Name { get; }
    public string FullPath { get; }
    public bool IsDirectory { get; }
    public string SizeText { get; }
    public string CreatedText { get; }
    public string ModifiedText { get; }
    public string IconKind { get; }

    public FileItem(string path) {
        FullPath = path;
        Name = Path.GetFileName(path);
        IsDirectory = Directory.Exists(path);

        // Default fallbacks
        var sizeText = "0 items";
        var createdText = "-";
        var modifiedText = "-";
        var iconKind = IsDirectory ? GetFolderIcon(Name) : GetFileIcon(Name);

        try {
            if (IsDirectory) {
                var count = Directory.EnumerateFileSystemEntries(path).Count();
                sizeText = $"{count} items";
            }
            else {
                var info = new FileInfo(path);
                sizeText = FormatFileSize(info.Length);
            }
        }
        catch {
            sizeText = "Unknown";
        }

        try {
            // FileSystemInfo handles both Files and Directories for creation/modification times
            var fsInfo = new FileInfo(path);
            if (IsDirectory) {
                var dirInfo = new DirectoryInfo(path);
                createdText = dirInfo.CreationTime.ToString("MM/dd/yy, h:mm tt");
                modifiedText = dirInfo.LastWriteTime.ToString("MM/dd/yy, h:mm tt");
            } else {
                createdText = fsInfo.CreationTime.ToString("MM/dd/yy, h:mm tt");
                modifiedText = fsInfo.LastWriteTime.ToString("MM/dd/yy, h:mm tt");
            }
        }
        catch {
            createdText = "-";
            modifiedText = "-";
        }

        SizeText = sizeText;
        CreatedText = createdText;
        ModifiedText = modifiedText;
        IconKind = iconKind;
    }

    private static string FormatFileSize(long bytes) {
        string[] suffixes = ["B", "KB", "MB", "GB"];
        var counter = 0;
        decimal number = bytes;
        while (Math.Round(number / 1024) >= 1) {
            number /= 1024;
            counter++;
        }
        return $"{number:n1} {suffixes[counter]}";
    }

    private static string GetFolderIcon(string name) => name.ToLower() switch {
        "config" => "FolderConfig",
        "mods" => "FolderMods",
        "saves" => "FolderSaves",
        "resourcepacks" => "FolderResourcePacks",
        "shaderpacks" => "FolderShaderPacks",
        _ => "Folder"
    };

    private static string GetFileIcon(string name) => Path.GetExtension(name).ToLower() switch {
        ".json" => "FileJson",
        ".zip" or ".jar" => "FileArchive",
        _ => "File"
    };
}