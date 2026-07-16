using System.Collections.Generic;
using System.IO;
using System.Linq;
using ModpackInstaller.Models.FileSistem;

namespace ModpackInstaller.Services.FileSistem;

public class FileSystemService {
    public static IEnumerable<TreeNodeInfo> GetDirectoryContents(string path, string basePath) {
        var fileInfoModels = new List<TreeNodeInfo>();

        if (!Directory.Exists(path)) {
            return fileInfoModels;
        }

        fileInfoModels.AddRange(
            Directory.GetDirectories(path)
                .Select(directory => 
                    new TreeNodeInfo {
                        FullPath = directory, 
                        RelativePath = Path.GetRelativePath(basePath, directory), 
                        Name = Path.GetFileName(directory), 
                        IsDirectory = true
                    }));

        fileInfoModels.AddRange(
            Directory.GetFiles(path)
                .Select(file => 
                    new TreeNodeInfo {
                        FullPath = file, 
                        RelativePath = Path.GetRelativePath(basePath, file), 
                        Name = Path.GetFileName(file), 
                        IsDirectory = false
                    }));

        return fileInfoModels.OrderBy(f => f.Name);
    }
}