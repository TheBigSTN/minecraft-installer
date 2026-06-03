using System.Text.Json.Serialization;
using ModpackInstaller.Models.Modrinth;

namespace ModpackInstaller.Models;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ModSource {
    Local,      // Mod adăugat manual de utilizator (drag & drop)
    Remote,     // Mod instalat prin modpack (descărcat de pe internet)
    CustomUrl   // Mod instalat via un link direct
}

public class ModInfo {
    public string ProjectId { get; set; } = "";
    public string VersionId { get; set; } = "";
    public string VersionNumber { get; set; } = "";
    public ModSource Source { get; set; } = ModSource.Local;
    public string Title { get; set; } = "";
    public string Filename { get; set; } = "";
    public string DownloadUrl { get; set; } = "";
    public string IconUrl { get; set; } = "";
    public bool Enabled { get; set; } = true;

    public SideSupport ClientSide { get; set; } = SideSupport.unknown;
    public SideSupport ServerSide { get; set; } = SideSupport.unknown;

    public string FileSha { get; set; } = "";

    // You need to add them in the backend otherwize update pathches won't work
}