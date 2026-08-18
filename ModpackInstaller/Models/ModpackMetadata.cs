using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using ModpackInstaller.Converters;
using ModpackInstaller.Models.Interfaces;
using ModpackInstaller.Services;
using ModpackInstaller.Services.Helpers;
using ModpackInstaller.ViewModels.Dialogs;

namespace ModpackInstaller.Models;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ModpackSource {
    Local,
    Remote
}

public class ModpackMetadata : IMigratedData, IReadOnlyModpackMetadata {
    public Guid Id { get; set; }

    public Guid? ModpackId { get; set; }
    public string Name { get; set; } = null!;

    public Guid VersionId { get; set; } = Guid.Empty;

    [JsonConverter(typeof(StringConverter))]
    public string VersionSemver { get; set; } = null!;

    public string? SharingCode { get; set; }
    public string? ModpackPassword { get; set; }

    public ModpackSource Source { get; set; } = ModpackSource.Local;

    public string GameVersion { get; set; } = null!;
    public ModLoaderType Loader { get; set; }
    public string LoaderVersion { get; set; } = null!;

    public string Author { get; set; } = null!;
    public string? Description { get; set; }

    public string InstallPath { get; set; } = null!;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public bool IsServerInstall { get; set; }

    public string Icon { get; init; } = null!;

    public static int SchemaVersion => 1;

    public static void Migrate(JsonObject root, int fileVersion) {
        if (fileVersion < 1) {
            NormalizeGuid(root, nameof(Id));
            NormalizeGuid(root, nameof(ModpackId));
            NormalizeGuid(root, nameof(VersionId));
        }
    }
    
    private static void NormalizeGuid(JsonObject root, string property) {
        if (root[property]?.GetValue<string>() is not { } value)
            return;

        if (!Guid.TryParse(value, out var guid))
            return;

        root[property] = guid.ToString("D");
    }
}