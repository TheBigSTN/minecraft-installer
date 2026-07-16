using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModpackInstaller.Models;

public class ModpackManifest {
    public List<ModInfo> InstalledMods { get; set; } = [];
}

public sealed class ManifestDiff {
    public List<ModInfo> Added { get; } = [];
    public List<ModInfo> Removed { get; } = [];
    public List<ManifestUpdate> Updated { get; } = [];
}

public sealed class ManifestUpdate {
    public required ModInfo OldMod { get; init; }
    public required ModInfo NewMod { get; init; }
}