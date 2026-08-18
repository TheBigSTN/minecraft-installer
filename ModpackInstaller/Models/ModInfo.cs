using System;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using ModpackInstaller.Infrastructure;
using ModpackInstaller.Models.Caches;
using ModpackInstaller.Services;
using ModpackInstaller.Services.Helpers;
using Environment = ModpackInstaller.Models.Modrinth.Environment;

namespace ModpackInstaller.Models;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ModSource {
    Local,
    Remote,
    CustomUrl
}

public class ModInfo : IMigratedData, IModVersion {
    public required string ProjectId { get; set; }
    public required string VersionId { get; set; }
    public string VersionNumber { get; set; } = "";
    public ModSource Source { get; set; } = ModSource.Local;
    public string Title { get; set; } = "";
    public string Filename { get; set; } = "";
    public string DownloadUrl { get; set; } = "";
    public string IconUrl { get; set; } = "";
    public bool Enabled { get; set; } = true;
    public string FileSha { get; set; } = "";
    public string OwnerName { get; set; } = "";
    public required Environment Environment { get; set; } = Environment.Unknown;
    public static int SchemaVersion => 1;

    public static void Migrate(JsonObject root, int fileVersion) {
        if (fileVersion < 1) {
            var versionId = root.GetField<string>(nameof(VersionId));
            var source = root.GetField<ModSource>(nameof(Source));

            if (!string.IsNullOrEmpty(versionId)) {
                if (source is ModSource.Remote) {
                    var version = ModrinthVersionCache.Get(versionId);
                    
                    root.SetField(nameof(Environment), version?.Environment ?? Environment.HaveToRequest);
                    
                }
                else {
                    root.SetField(nameof(Environment), Environment.ClientAndServer);
                }
                
            }
        }
    }
}

public enum ModInstallState {
    NotInstalled,
    InstalledSameVersion,
    InstalledDifferentVersion
}

public interface IModVersion {
    string ProjectId { get; }
    string VersionId { get; }
}

public class ModVersion(string projectId, string versionId) : IModVersion {
    public string ProjectId { get; } = projectId;
    public string VersionId { get; } = versionId;
}