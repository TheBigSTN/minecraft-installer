using System.Linq;
using ModpackInstaller.Models;

namespace ModpackInstaller.Services.Modpack;

public static class ManifestDiffCreator {

    public static ManifestDiff Create(
        ModpackManifest oldManifest,
        ModpackManifest newManifest)
    {
        var diff = new ManifestDiff();

        var oldMods = oldManifest.InstalledMods.ToDictionary(x => x.ProjectId);
        var newMods = newManifest.InstalledMods.ToDictionary(x => x.ProjectId);

        foreach (var (projectId, newMod) in newMods)
        {
            if (!oldMods.TryGetValue(projectId, out var oldMod))
            {
                diff.Added.Add(newMod);
                continue;
            }

            if (oldMod.VersionId != newMod.VersionId)
            {
                diff.Updated.Add(new ManifestUpdate
                {
                    OldMod = oldMod,
                    NewMod = newMod
                });
            }
        }

        foreach (var (projectId, oldMod) in oldMods)
        {
            if (!newMods.ContainsKey(projectId))
                diff.Removed.Add(oldMod);
        }

        return diff;
    }
}