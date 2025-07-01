using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using static ModpackInstaller.Services.ModpackService;

namespace ModpackInstaller.Services;

public class Modpack {
    public readonly string installLocation;
    public readonly string tlauncerAdditionalPath;
    public readonly string mineloaderAdditionalPath;
    public readonly string modpackId;

    public Modpack(string modpackId) {
        installLocation = Path.Join(ModpackService.modpacksPath, modpackId);
        this.modpackId = modpackId;
        tlauncerAdditionalPath = Path.Combine(installLocation, "TLauncherAdditional.json");
        mineloaderAdditionalPath = Path.Combine(installLocation, "mineloader-Aditional.json");
    }

    public async Task InstallModpack() {
        string modpackSha = await GetModpackSha();
        var mineLoaderData = GetMineLoaderData();

        await Github.DownloadModpack(modpackSha, modpackId, mineLoaderData.AutoUpdate);
    }

    public async Task UpdateModpack() {
        await ModpackUpdater.Update(modpackId);
    }

    public async Task<bool> IsOutdated() {
        MineLoaderData mineLoaderData = GetMineLoaderData();

        string LatestSha = await GetModpackSha();

        return mineLoaderData.FileTree.Sha != LatestSha;
    }

    public async Task<String> GetModpackSha() {
        var allModpacks = await Github.GetAllRemoteModpacks();
        var modpack = allModpacks.Tree.Find(item => item.Path == modpackId);
        return modpack == null
            ? throw new InvalidOperationException($"Modpack with ID '{modpackId}' not found in the remote tree.")
            : modpack.Sha;
    }

    public async Task<GitHubTree> GetModpackRemoteTree() {
        var modpackSha = await GetModpackSha();
        return await Github.GetGitHubTreeAsync(true, modpackSha);
    }

    public ModpackService.Modpack GetInformation() {
        ModpackService.Modpack modpackdata = new();
        try {
            modpackdata.TLauncher = GetLauncherData();
           
            modpackdata.MineLoader = GetMineLoaderData();
            
        }
        catch (Exception ex) {
            Debug.WriteLine("An error occurred: " + ex.Message);
        }
        return modpackdata;
    }

    public TLauncherData GetLauncherData() {
        if (File.Exists(tlauncerAdditionalPath)) {
            string jsonContent = File.ReadAllText(tlauncerAdditionalPath);
            using JsonDocument doc = JsonDocument.Parse(jsonContent);
            JsonElement root = doc.RootElement;
            TLauncherData launcherData = ParseTLauncherData(root);
            return launcherData;
        } else 
            return new TLauncherData();
    }

    public MineLoaderData GetMineLoaderData() {
        if (File.Exists(mineloaderAdditionalPath)) {
            bool migration = false;
            using JsonDocument mineloaderDoc = JsonDocument.Parse(File.ReadAllText(mineloaderAdditionalPath));
            JsonElement root = mineloaderDoc.RootElement;

            int version = root.TryGetProperty("V", out var vElement) && vElement.TryGetInt32(out int v) ? v : 0;

            if ( version < 1 ) {
                migration = true;
                GitHubTree fallbackGitTree = new() {
                    Sha = "",
                    Url = "",
                    Truncated = false,
                    Tree = [
                       new GitHubTreeItem { Mode = "", Path = "", Sha = "", Type = "", Url = "", Size = 0 }
                   ]
                };

                var migrated = new MineLoaderData {
                    ModpackName = root.TryGetProperty("ModpackName", out var name) ? name.GetString()! : "",
                    FileTree = root.TryGetProperty("Tree", out var oldTree) ? oldTree.Deserialize<GitHubTree>()! : fallbackGitTree,
                    AutoUpdate = false,
                    V = 1
                };

                // Scrii înapoi versiunea actualizată, dacă vrei
                using var serialized = JsonSerializer.SerializeToDocument(migrated, ModpackService.jsonOptions);
                root = serialized.RootElement.Clone();
            }

            if (migration) {
                string serialized = JsonSerializer.Serialize(
                    root.Deserialize<MineLoaderData>(), 
                    ModpackService.jsonOptions
                );
                File.WriteAllText(mineloaderAdditionalPath, serialized);
            }

            MineLoaderData? mineLoaderData = root.Deserialize<MineLoaderData>();
            return mineLoaderData ?? new MineLoaderData();
        } else 
            return new MineLoaderData();
    }

}

