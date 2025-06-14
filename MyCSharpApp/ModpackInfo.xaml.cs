using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Xml.Linq;
using Github;
using Html.Web;
using MyCSharpApp;
using static MyCSharpApp.Modpacks;

namespace MyCSharpApp {

    /// <summary>
    /// Interaction logic for ModpackInfo.xaml
    /// </summary>
    public partial class ModpackInfo : UserControl {
        private string modpackName;
        public ModpackInfo(Modpacks.Modpack modpack) {
            InitializeComponent();
            ModpackNameText.Text = modpack.MineLoader.ModpackName;
            modpackName = modpack.MineLoader.ModpackName;
        }

        private async void CheckUpdate_Click(object sender, RoutedEventArgs e) {
            // 1. Get remote Git tree and local modpack data.
            GitHubTree remoteTree = await GithubHelper.GetModpackWithName(modpackName);
            Modpacks.Modpack localModpack = Modpacks.GetModpack(modpackName);
            string jsonFilePath = System.IO.Path.Combine(Modpacks.modpacksPath, modpackName, "TLauncherAdditional.json");

            // Assuming localModpack.MineLoader has a "Tree" property of type GitHubTree.
            if (remoteTree.Sha != localModpack.MineLoader.Tree.Sha) {
                var result = MessageBox.Show(
                    "An update is available for this modpack.\nDo you want to update now?",
                    "Update Available",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes) {
                    UpdateModpack(remoteTree, localModpack, jsonFilePath);
                }
            }
            else {
                MessageBox.Show("No update is available.", "Up to date", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private async void UpdateModpack(GitHubTree remoteTree, Modpack localModpack, string jsonFilePath) {

            // 2. Diff remote vs. local MineLoader tree.
            List<GitHubTreeItem> remoteFiles = remoteTree.Tree;          // Remote list
            List<GitHubTreeItem> localFiles = localModpack.MineLoader.Tree.Tree; // Local tree items

            // Files/Folders to add/update: those in remote but either not present locally or with differing SHA.
            List<GitHubTreeItem> filesToDownload = remoteFiles
                .Where(r => !localFiles.Any(l => l.Path.Equals(r.Path, StringComparison.OrdinalIgnoreCase) && l.Sha == r.Sha))
                .ToList();

            // Files/Folders to delete: those present locally but not in remote.
            List<GitHubTreeItem> filesToDelete = localFiles
                .Where(l => !remoteFiles.Any(r => r.Path.Equals(l.Path, StringComparison.OrdinalIgnoreCase)))
                .ToList();

            ProcessDeletedFiles(filesToDelete);

            // 4. Process additions/updates.
            await DownloadNewFiles(filesToDownload);

            // 5. Update the local MineLoader tree reference so subsequent comparisons work.
            // (Assume you have a method that re-parses and sets the updated tree after download.)
            localModpack.MineLoader.Tree = remoteTree; // or re-read from disk if needed

            using JsonDocument remoteTLauncherData = await GithubHelper.GetGitLauncherDataAsync(remoteTree.Sha, modpackName);

            JsonSerializerOptions options = new JsonSerializerOptions { WriteIndented = true };

            var data = new Modpacks.MineLoaderData(
                modpackName,
                ParseTLauncherData(remoteTLauncherData.RootElement).Mods,
                remoteTree
            );
            try {
                // 5. Update TLauncher file. (The hard part)
                await UpdateTlauncerFile(localModpack, jsonFilePath, remoteTLauncherData);
            }
            catch (Exception ex) {
                MessageBox.Show("Eroare la procesare: " + ex.Message);
                return;
            }

            // 6. Update Mineloader File
            using FileStream mineloaderWriteStream = File.Create(System.IO.Path.Combine(Modpacks.modpacksPath, modpackName, "mineloader-Aditional.json"));

            // 7. Write files to disc
            await JsonSerializer.SerializeAsync(mineloaderWriteStream, data, options);
            MessageBox.Show("Update complete!", "Done", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private static async Task UpdateTlauncerFile(Modpack localModpack, string jsonFilePath, JsonDocument remoteTLauncherData) {
            var mineloaderMods = localModpack.MineLoader.Mods;

            // Step 1: Parse the modifiable rootNode from file
            string jsonContent = File.ReadAllText(jsonFilePath);
            JsonNode? rootNode = JsonNode.Parse(jsonContent);

            JsonArray? localModsArray = rootNode?["modpack"]?["version"]?["mods"]?.AsArray();
            if (localModsArray is null) {
                MessageBox.Show("Nu s-a putut accesa rootNode[\"modpack\"][\"version\"][\"mods\"].");
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
                MessageBox.Show("Nu s-au găsit mods în remoteTLauncherData.");
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
                JsonNode remoteMod = remoteEntry.Value;

                if (!mineloaderPaths.Contains(path)) {
                    JsonNode clonedNode = JsonNode.Parse(remoteMod.ToJsonString())!;
                    localModsArray.Add(clonedNode);
                }
            }

            // Step 6: Save the updated rootNode back to file
            File.WriteAllText(jsonFilePath, rootNode.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
        }

        private async Task DownloadNewFiles(List<GitHubTreeItem> filesToDownload) {
            var headers = new Dictionary<string, string> {
                        { "Authorization", "Bearer " + GithubHelper.Token } // Use your token
                    };

            string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string modpackPath = System.IO.Path.Combine(appDataPath, ".minecraft", "versions", modpackName);

            // Make sure the base directory exists.
            Directory.CreateDirectory(modpackPath);

            var downloadTasks = new List<Task>();

            foreach (var item in filesToDownload) {
                // If the item is a directory (tree), create it.
                if (item.Type == "tree") {
                    string dirPath = System.IO.Path.Combine(modpackPath, item.Path);
                    Directory.CreateDirectory(dirPath);
                }
                // If it's a blob (file), download it.
                else if (item.Type == "blob") {
                    // Build the destination path.
                    string destPath = System.IO.Path.Combine(modpackPath, item.Path);

                    if (item.Path == "TLauncherAdditional.json") continue;
                    // Download the file.
                    downloadTasks.Add(Exiom.GetFile(item.Url, destPath, headers));

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

        private void ProcessDeletedFiles(List<GitHubTreeItem> filesToDelete) {
            foreach (var item in filesToDelete) {
                // Calculate the full path to the file/folder.
                string localPath = System.IO.Path.Combine(Modpacks.modpacksPath, modpackName, item.Path);
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
}
