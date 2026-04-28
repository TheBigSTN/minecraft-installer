using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace ModpackInstaller.Models.Modrinth;

public class ModrinthVersion {
    [JsonPropertyName("id")] public string Id { get; set; } = "";
    [JsonPropertyName("project_id")] public string ProjectId { get; set; } = "";
    [JsonPropertyName("name")] public string Name { get; set; } = "";
    [JsonPropertyName("version_number")] public string VersionNumber { get; set; } = "";
    [JsonPropertyName("version_type")] public string VersionType { get; set; } = ""; // release, beta, alpha
    [JsonPropertyName("loaders")] public List<string> Loaders { get; set; } = [];
    [JsonPropertyName("game_versions")] public List<string> GameVersions { get; set; } = [];
    [JsonPropertyName("files")] public List<ModrinthFile> Files { get; set; } = [];
    [JsonPropertyName("dependencies")] public List<ModrinthDependency> Dependencies { get; set; } = [];

    public ModrinthFile? PrimaryFile => Files.FirstOrDefault(f => f.IsPrimary) ?? Files.FirstOrDefault();
}

public class ModrinthFile {
    [JsonPropertyName("url")] public string Url { get; set; } = "";
    [JsonPropertyName("filename")] public string Filename { get; set; } = "";
    [JsonPropertyName("primary")] public bool IsPrimary { get; set; }
    [JsonPropertyName("size")] public long Size { get; set; }
    [JsonPropertyName("hashes")] public ModrinthFileHashes Hashes { get; set; } = new();
}

public class ModrinthDependency {
    [JsonPropertyName("project_id")] public string? ProjectId { get; set; }
    [JsonPropertyName("version_id")] public string? VersionId { get; set; }
    [JsonPropertyName("dependency_type")] public string DependencyType { get; set; } = ""; // required, optional, etc.
}

public class ModrinthFileHashes {
    [JsonPropertyName("sha1")]
    public string Sha1 { get; set; } = "";

    [JsonPropertyName("sha512")]
    public string Sha512 { get; set; } = "";
}