using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Avalonia.Controls;
using MsBox.Avalonia.Dto;
using MsBox.Avalonia.Enums;
using MsBox.Avalonia;

namespace ModpackInstaller.Services;
public static class ModpackUpdater {
    public async static Task Update(String modpackId) {
        Modpack modpack = new(modpackId);

        ModpackService.Modpack localModpack = modpack.GetInformation();
        GitHubTree remoteTree = await modpack.GetModpackRemoteTree();
        // 2. Diff remote vs. local MineLoader tree.
        List<GitHubTreeItem> remoteFiles = remoteTree.Tree;          // Remote list
        List<GitHubTreeItem> localFiles = localModpack.MineLoader.FileTree.Tree; // Local tree items

        // Files/Folders to add/update: those in remote but either not present locally or with differing SHA.
        List<GitHubTreeItem> filesToDownload = remoteFiles
            .Where(r => !localFiles.Any(l => l.Path.Equals(r.Path, StringComparison.OrdinalIgnoreCase) && l.Sha == r.Sha))
            .ToList();

        // Files/Folders to delete: those present locally but not in remote.
        List<GitHubTreeItem> filesToDelete = localFiles
            .Where(l => !remoteFiles.Any(r => r.Path.Equals(l.Path, StringComparison.OrdinalIgnoreCase)))
            .ToList();

        ProcessDeletedFiles(filesToDelete, modpackId);

        // 4. Process additions/updates.
        await DownloadNewFiles(filesToDownload, modpack.installLocation);

        // 5. Update the local MineLoader tree reference so subsequent comparisons work.
        // (Assume you have a method that re-parses and sets the updated tree after download.)
        localModpack.MineLoader.FileTree = remoteTree; // or re-read from disk if needed

        using JsonDocument remoteTLauncherData = await Github.GetGitLauncherDataAsync(remoteTree.Sha, modpackId);

        JsonSerializerOptions options = new() { WriteIndented = true };

        var data = new ModpackService.MineLoaderData(
            modpackId,
            ModpackService.ParseTLauncherData(remoteTLauncherData.RootElement).Mods,
            remoteTree
        );
        try {
            // 5. Update TLauncher file. (The hard part)
            await UpdateTlauncerFile(localModpack, modpack.tlauncerAdditionalPath, remoteTLauncherData);
        }
        catch (Exception ex) {
            var mbe = MessageBoxManager
                .GetMessageBoxStandard(new MessageBoxStandardParams {
                    ButtonDefinitions = ButtonEnum.Ok,
                    Icon = Icon.Error,
                    ContentTitle = "Eroare",
                    ContentMessage = "Eroare la procesare: " + ex.Message,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner
                });

            await mbe.ShowAsync();
            return;
        }

        // 6. Update Mineloader File
        using FileStream mineloaderWriteStream = File.Create(modpack.mineloaderAdditionalPath);

        // 7. Write files to disc
        await JsonSerializer.SerializeAsync(mineloaderWriteStream, data, options);

        var mb = MessageBoxManager
            .GetMessageBoxStandard(new MessageBoxStandardParams {
                ButtonDefinitions = ButtonEnum.Ok,
                Icon = Icon.Info,
                ContentTitle = "Update complete!",
                ContentMessage = "Done",
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            });

        await mb.ShowAsync();
    }

    private static async Task UpdateTlauncerFile(ModpackService.Modpack localModpack, string jsonFilePath, JsonDocument remoteTLauncherData) {
        var mineloaderMods = localModpack.MineLoader.Mods;

        // Step 1: Parse the modifiable rootNode from file
        string jsonContent = File.ReadAllText(jsonFilePath);
        JsonNode? rootNode = JsonNode.Parse(jsonContent);

        JsonArray? localModsArray = rootNode?["modpack"]?["version"]?["mods"]?.AsArray();
        if (localModsArray is null) {

            var msg = MessageBoxManager
                    .GetMessageBoxStandard(new MessageBoxStandardParams {
                        ButtonDefinitions = ButtonEnum.Ok,
                        Icon = Icon.Error,
                        ContentTitle = "Eroare",
                        ContentMessage = "Nu s-a putut accesa rootNode[\"modpack\"][\"version\"][\"mods\"].",
                        WindowStartupLocation = WindowStartupLocation.CenterOwner
                    });

            await msg.ShowAsync();
            return;
        }

        // Step 2: Extract remote mods from JsonElement
        JsonArray? remoteModsArray = null;
        if (remoteTLauncherData.RootElement.TryGetProperty("modpack", out JsonElement remoteModpack) &&
            remoteModpack.TryGetProperty("version", out JsonElement remoteVersion) &&
            remoteVersion.TryGetProperty("mods", out JsonElement remoteMods) &&
            remoteMods.ValueKind == JsonValueKind.Array) {
            remoteModsArray = (JsonArray)JsonNode.Parse(remoteMods.GetRawText())!;
        }
        else {
            var msg = MessageBoxManager
        .GetMessageBoxStandard(new MessageBoxStandardParams {
            ButtonDefinitions = ButtonEnum.Ok,
            Icon = Icon.Error,
            ContentTitle = "Eroare",
            ContentMessage = "Nu s-au găsit mods în remoteTLauncherData.",
            WindowStartupLocation = WindowStartupLocation.CenterOwner
        });

            await msg.ShowAsync();
            return;
        }

        // Step 3: Build lookup sets
        var mineloaderPaths = new HashSet<string>(
            mineloaderMods
                .Where(mod => mod.Path != null)
                .Select(mod => mod.Path.ToString()!)
        );

        var remoteModLookup = remoteModsArray
            .Where(remoteMod => remoteMod?["version"]?["metadata"]?["path"] != null)
            .GroupBy(mod => mod!["version"]!["metadata"]!["path"]!.ToString()!)
            .ToDictionary(
                group => group.Key,
                group => group.First() // ia primul mod cu acel path
            );

        // Step 4: Remove mods that exist in mineloaderMods but not in remote
        var modsToRemove = localModsArray
            .Where(mod => {
                string? path = mod?["Path"]?.ToString();
                return path != null && mineloaderPaths.Contains(path) && !remoteModLookup.ContainsKey(path);
            })
            .ToList();

        foreach (var mod in modsToRemove) {
            localModsArray.Remove(mod);
        }

        // Step 5: Add mods that exist in remote but not in mineloader
        foreach (var remoteEntry in remoteModLookup) {
            string path = remoteEntry.Key;
            JsonNode? remoteMod = remoteEntry.Value;
            if (remoteMod is null) {
                continue; // Skip this iteration if remoteMod is null  
            }

            if (!mineloaderPaths.Contains(path)) {
                JsonNode clonedNode = JsonNode.Parse(remoteMod.ToJsonString())!;
                localModsArray.Add(clonedNode);
            }
        }

        // Step 6: Save the updated rootNode back to file
        File.WriteAllText(jsonFilePath, rootNode?.ToJsonString(new JsonSerializerOptions { WriteIndented = true }) ?? throw new InvalidOperationException("rootNode is null."));
    }

    private async static Task DownloadNewFiles(List<GitHubTreeItem> filesToDownload, String modpackInstallPath) {
        var headers = new Dictionary<string, string> {
                        { "Authorization", "Bearer " + Github.Token }
                    };

        // Make sure the base directory exists.
        Directory.CreateDirectory(modpackInstallPath);

        var downloadTasks = new List<Task>();

        foreach (var item in filesToDownload) {
            // If the item is a directory (tree), create it.
            if (item.Type == "tree") {
                string dirPath = System.IO.Path.Combine(modpackInstallPath, item.Path);
                Directory.CreateDirectory(dirPath);
            }
            // If it's a blob (file), download it.
            else if (item.Type == "blob") {
                // Build the destination path.
                string destPath = System.IO.Path.Combine(modpackInstallPath, item.Path);

                if (item.Path == "TLauncherAdditional.json") continue;
                // Download the file.
                downloadTasks.Add(WebService.GetFile(item.Url, destPath, headers));

                // For performance you could batch these tasks.
                if (downloadTasks.Count == 10) {
                    await Task.WhenAll(downloadTasks);
                    downloadTasks.Clear();
                }
            }
        }
        if (downloadTasks.Count > 0)
            await Task.WhenAll(downloadTasks);
    }

    private static void ProcessDeletedFiles(List<GitHubTreeItem> filesToDelete, String modpackName) {
        foreach (var item in filesToDelete) {
            // Calculate the full path to the file/folder.
            string localPath = System.IO.Path.Combine(ModpackService.modpacksPath, modpackName, item.Path);
            try {
                if (item.Path == "TLauncherAdditional.json") continue;
                if (item.Type == "blob" && File.Exists(localPath))
                    File.Delete(localPath);
                else if (item.Type == "tree" && Directory.Exists(localPath))
                    Directory.Delete(localPath, true); // delete recursively
            }
            catch (Exception ex) {
                // Log or handle deletion exceptions.
                Debug.WriteLine($"Failed to delete {localPath}: " + ex.Message);
            }
        }
    }
}
