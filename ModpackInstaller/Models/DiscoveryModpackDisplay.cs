using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ModpackInstaller.Models.DTOs;

namespace ModpackInstaller.Models;
public class DiscoveryModpackDisplay : PublicModpackRequestResponse {
    public required bool IsInstalled { get; set; }

    [SetsRequiredMembers]
    public DiscoveryModpackDisplay( PublicModpackRequestResponse baseData, bool isInstalled ) {
        Id = baseData.Id;
        ModpackName = baseData.ModpackName;
        AuthorName = baseData.AuthorName;
        GameVersion = baseData.GameVersion;
        Loader = baseData.Loader;
        LoaderVersion = baseData.LoaderVersion;
        LatestVersion = baseData.LatestVersion;
        CreatedAt = baseData.CreatedAt;
        ModifiedAt = baseData.ModifiedAt;
        Description = baseData.Description;

        IsInstalled = isInstalled;
    }
}
