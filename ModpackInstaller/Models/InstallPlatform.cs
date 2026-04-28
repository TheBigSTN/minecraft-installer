using System.Text.Json.Serialization;

namespace ModpackInstaller.Models;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum InstallPlatform {
    TLauncher,
    CurseForge,
    Modrinth,
    Custom
}
