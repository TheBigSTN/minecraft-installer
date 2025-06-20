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
    public readonly String installLocation;
    public readonly String tlauncerAdditionalPath;
    public readonly String mineloaderAdditionalPath;
    public readonly String modpackId;

    public Modpack(String modpackId) {
        installLocation = Path.Join(ModpackService.modpacksPath, modpackId);
        this.modpackId = modpackId;
        tlauncerAdditionalPath = Path.Combine(installLocation, "TLauncherAdditional.json");
        mineloaderAdditionalPath = Path.Combine(installLocation, "mineloader-Aditional.json");
    }

    public async Task InstallModpack() {
        String modpackSha = await GetModpackSha();

        await Github.DownloadModpack(modpackSha, modpackId);
    }

    public async Task UpdateModpack() {
        await ModpackUpdater.Update(modpackId);
    }

    public async Task<bool> IsOutdated() {
        MineLoaderData mineLoaderData = GetMineLoaderData();

        String LatestSha = await GetModpackSha();

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

    public ModpackService.TLauncherData GetLauncherData() {
        if (File.Exists(tlauncerAdditionalPath)) {
            string jsonContent = File.ReadAllText(tlauncerAdditionalPath);
            using JsonDocument doc = JsonDocument.Parse(jsonContent);
            JsonElement root = doc.RootElement;
            ModpackService.TLauncherData launcherData = ModpackService.ParseTLauncherData(root);
            return launcherData;
        } else 
            return new ModpackService.TLauncherData();
    }

    public ModpackService.MineLoaderData GetMineLoaderData() {
        if (File.Exists(mineloaderAdditionalPath)) {
            string mineloaderContent = File.ReadAllText(mineloaderAdditionalPath);
            using JsonDocument mineloaderdoc = JsonDocument.Parse(mineloaderContent);
            JsonElement mineloaderroot = mineloaderdoc.RootElement;
            ModpackService.MineLoaderData? mineLoaderData = mineloaderroot.Deserialize<ModpackService.MineLoaderData>();
            return mineLoaderData ?? new ModpackService.MineLoaderData();
        } else 
            return new ModpackService.MineLoaderData();
    }

}

