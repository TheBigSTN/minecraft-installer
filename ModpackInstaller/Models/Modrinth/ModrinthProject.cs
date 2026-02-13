using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace ModpackInstaller.Models.Modrinth;

public class ModrinthProject {
    [JsonPropertyName("id")]
    public string Id { get; set; } = "";

    [JsonPropertyName("title")]
    public string Title { get; set; } = "";

    [JsonPropertyName("description")]
    public string Description { get; set; } = "";

    [JsonPropertyName("project_id")]
    public string ProjectIdSearch {
        get => Id;
        set => Id = value;
    }

    [JsonPropertyName("icon_url")]  
    public string IconURL { get; set; } = "";
}
