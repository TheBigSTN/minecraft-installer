using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModpackInstaller.Models.DTOs;

public class PublicModpackRequestResponse {

    public required string Id { get; set; }
    public required string ModpackName { get; set; }
    public required string AuthorName { get; set; }
    public required string GameVersion { get; set; }
    public required ModLoaderType Loader { get; set; }
    public required string LoaderVersion { get; set; }
    public required int    LatestVersion { get; set; }
    public required DateTimeOffset CreatedAt { get; set; }
    public required DateTimeOffset ModifiedAt { get; set; }
    public string Description { get; set; } = "";
}
