using System.Text.Json.Serialization;
using ModpackInstaller.Models.Modrinth;

namespace ModpackInstaller.Models.Modrinth;

public class ModrinthTeamMember {
    [JsonPropertyName("team_id")]
    public required string TeamId { get; set; }
    [JsonPropertyName("user")]
    public required ModrinthUser User { get; set; }
    [JsonPropertyName("role")]
    public required string Role { get; set; }
    
}