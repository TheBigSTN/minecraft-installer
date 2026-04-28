using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ModpackInstaller.Models;
using ModpackInstaller.Models.Modrinth;

namespace ModpackInstaller.Services;

public class ModrinthApiService {
    private const string BaseUrl = "https://api.modrinth.com/v2";

    private static readonly Dictionary<string, string> DefaultHeaders = new() {
        ["User-Agent"] = "ModpackInstaller/1.0"
    };

    private static string GetManifestUrl(ModLoaderType loader) => loader switch {
        ModLoaderType.Forge => "https://launcher-meta.modrinth.com/forge/v0/manifest.json",
        ModLoaderType.NeoForge => "https://launcher-meta.modrinth.com/neo/v0/manifest.json",
        ModLoaderType.Fabric => "https://launcher-meta.modrinth.com/fabric/v0/manifest.json",
        ModLoaderType.Quilt => "https://launcher-meta.modrinth.com/quilt/v0/manifest.json",
        _ => throw new NotSupportedException(loader.ToString())
    };

    // 🔹 Game Versions
    public async Task<IReadOnlyList<string>> GetGameVersionsAsync(
        ModLoaderType loader,
        bool stableOnly = true) {
        var url = GetManifestUrl(loader);

        var manifest = await WebService.GetJson<LauncherManifestDto>(url, DefaultHeaders);
        if (manifest == null)
            return [];

        var versions = manifest.GameVersions
            // ❌ scoate placeholder-ul
            .Where(v => !v.Id.StartsWith("$"));

        if (stableOnly)
            versions = versions.Where(v => v.Stable);

        return versions
            .Select(v => v.Id)
            .ToList();
    }

    // 🔹 Loader versions (Forge / Fabric / NeoForge)
    public async Task<IReadOnlyList<string>> GetLoaderVersionsAsync(
        ModLoaderType loader,
        string gameVersion,
        bool stableOnly = true) {
        var url = GetManifestUrl(loader);

        var manifest = await WebService.GetJson<LauncherManifestDto>(url, DefaultHeaders);
        if (manifest == null)
            return [];

        // 1️⃣ încearcă versiunea specifică
        var gv = manifest.GameVersions
            .FirstOrDefault(v => v.Id == gameVersion);

        // 2️⃣ fallback Fabric / Quilt
        gv ??= manifest.GameVersions
            .FirstOrDefault(v => v.Id.StartsWith("$"));

        if (gv == null)
            return [];

        var loaders = gv.Loaders.AsEnumerable();

        // 3️⃣ stable inteligent
        if (stableOnly) {
            var stableLoaders = loaders.Where(l => l.Stable).ToList();

            if (stableLoaders.Any())
                loaders = stableLoaders;
            // altfel: păstrează TOATE (Fabric / Quilt)
        }

        return loaders
            .Select(l => l.Id)
            .ToList();
    }

    public async Task<ModrinthVersion?> GetCompatibleVersionAsync(
        string projectSlugOrId,
        string gameVersion,
        ModLoaderType loader) // Folosim Enum-ul aici
    {
        // Convertim Enum-ul (Forge, Fabric etc.) în string-ul așteptat de API (forge, fabric)
        string loaderStr = loader.ToString().ToLower();

        var loadersJson = Uri.EscapeDataString($"[\"{loaderStr}\"]");
        var gameVersionsJson = Uri.EscapeDataString($"[\"{gameVersion}\"]");

        var url = $"{BaseUrl}/project/{projectSlugOrId}/version" +
                  $"?loaders={loadersJson}&game_versions={gameVersionsJson}&include_changelog=false";

        var versions = await WebService.GetJson<List<ModrinthVersion>>(url, DefaultHeaders);
        return versions?.OrderByDescending(v => v.VersionType == "release").FirstOrDefault();
    }
    public async Task<ModrinthVersion?> GetVersionAsync(string versionId) {
        var url = $"{BaseUrl}/version/{versionId}";
        return await WebService.GetJson<ModrinthVersion>(url, DefaultHeaders);
    }

    public static async Task<ModrinthProject?> GetProjectAsync(string projectIdOrSlug) {
        var url = $"{BaseUrl}/project/{projectIdOrSlug}";
        return await WebService.GetJson<ModrinthProject>(url, DefaultHeaders);
    }

    public static async Task<ModrinthVersion?> GetVersionByHashAsync( string sha1 ) {
        var url = $"{BaseUrl}/version_file/{sha1}";

        return await WebService.GetJson<ModrinthVersion>(url, DefaultHeaders);
    }
}
