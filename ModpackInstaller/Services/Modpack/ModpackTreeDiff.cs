using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ModpackInstaller.Models.DTOs;

namespace ModpackInstaller.Services.Modpack;

public sealed class ModpackTreeDiff {
    public List<ModpackFileDto> FilesToDownload { get; } = [];
    public List<ModpackFileDto> FilesToDelete { get; } = [];

    public static ModpackTreeDiff Create(
        ModpackTreeDto local,
        ModpackTreeDto remote) {
        var diff = new ModpackTreeDiff();

        var localFiles = local.Files.ToDictionary(x => x.FilePath);
        var remoteFiles = remote.Files.ToDictionary(x => x.FilePath);

        // Fișiere noi sau modificate
        foreach (var remoteFile in remote.Files)
        {
            if (!localFiles.TryGetValue(remoteFile.FilePath, out var localFile))
            {
                diff.FilesToDownload.Add(remoteFile);
                continue;
            }

            if (!string.Equals(localFile.Sha256, remoteFile.Sha256, StringComparison.Ordinal))
            {
                diff.FilesToDownload.Add(remoteFile);
            }
        }

        // Fișiere eliminate
        foreach (var localFile in local.Files)
        {
            if (!remoteFiles.ContainsKey(localFile.FilePath))
            {
                diff.FilesToDelete.Add(localFile);
            }
        }

        return diff;
    }
}