using System.Text.Json.Serialization;

namespace ModpackInstaller.Models.Modrinth;

public class ModrinthUser {
    [JsonPropertyName("id")]
    public required string Id { get; set; }
    [JsonPropertyName("username")]
    public required string Username { get; set; }
    [JsonPropertyName("avatar_url")]
    public required string AvatarUrl { get; set; }
}