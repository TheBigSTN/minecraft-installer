using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace ModpackInstaller.Models.Modrinth;

public class ModrinthSearchResponse {
    [JsonPropertyName("hits")]
    public List<ModrinthProject> Hits { get; set; } = new();
}