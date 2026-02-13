using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModpackInstaller.Models;
public sealed class LauncherManifestDto {
    public List<GameVersionEntryDto> GameVersions { get; set; } = [];
}

public sealed class GameVersionEntryDto {
    public string Id { get; set; } = "";
    public bool Stable { get; set; }

    public List<LoaderEntryDto> Loaders { get; set; } = [];
}

public sealed class LoaderEntryDto {
    public string Id { get; set; } = "";
    public string Url { get; set; } = ""; // păstrat pt viitor
    public bool Stable { get; set; }
}
