using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using ModpackInstaller.Infrastructure;
using ModpackInstaller.Services.Helpers;

namespace ModpackInstaller.Models;

public class ModpackManifest : IMigratedData {
    public List<ModInfo> InstalledMods { get; set; } = [];
    public static int SchemaVersion => 1;

    public static void Migrate(JsonObject root, int fileVersion) {

    }
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