using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace ModpackInstaller.Models;


[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ModpackSource {
    Local,
    Remote
}

public class ModpackMetadata {
    public string Id { get; set; } = default!;
    public string Name { get; set; } = default!;
    public int Version { get; set; } = 0;

    public string OwnerNickname { get; set; } = default!;
    public bool IsPublic { get; set; } = false;
    public string? SharingCode { get; set; }
    public string? ModpackPassword { get; set; }

    public ModpackSource Source { get; set; } = ModpackSource.Local;

    public string GameVersion { get; set; } = default!;
    public ModLoaderType Loader { get; set; }
    public string LoaderVersion { get; set; } = default!;

    public string Author { get; set; } = default!;
    public string? Description { get; set; }

    public string InstallPath { get; set; } = default!;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}