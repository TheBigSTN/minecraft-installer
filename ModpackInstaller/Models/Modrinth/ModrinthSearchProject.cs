using System;
using System.Text.Json.Serialization;

namespace ModpackInstaller.Models.Modrinth;

public class ModrinthSearchProject {

    [JsonPropertyName("project_id")] 
    public required string Id { get; set; }
    
    [JsonPropertyName("author")] 
    public required string Author { get; set; }

    [JsonPropertyName("title")] 
    public string? Title { get; set; }

    [JsonPropertyName("description")] 
    public string? Description { get; set; }

    [JsonPropertyName("client_side")] 
    public SideSupport ClientSide { get; set; } = SideSupport.unknown;

    [JsonPropertyName("server_side")]
    public SideSupport ServerSide { get; set; } = SideSupport.unknown;

    [JsonPropertyName("date_created")] 
    public DateTime CreationDate { get; set; }
    
    [JsonPropertyName("date_modified")] 
    public DateTime ModifiedDate { get; set; }
    
    [JsonPropertyName("icon_url")]  
    public string? IconUrl { get; set; }

    [JsonPropertyName("downloads")] 
    public required int Downloads { get; set; }
    
}