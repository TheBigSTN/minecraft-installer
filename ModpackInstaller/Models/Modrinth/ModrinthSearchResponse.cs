using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace ModpackInstaller.Models.Modrinth;

public class ModrinthSearchResponse {
    [JsonPropertyName("hits")]
    public List<ModrinthSearchProject> Hits { get; set; } = [];
    
    [JsonPropertyName("offset")]
    public required int Offset;
    
    [JsonPropertyName("limit")]
    public required int Limit;
    
    [JsonPropertyName("total_hits")]
    public required int TotalHits;
}