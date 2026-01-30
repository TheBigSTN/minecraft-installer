using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace ModpackInstaller.Services {
    public static class ModpackService {
        public static readonly string modpacksPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), ".minecraft", "versions");
        public static readonly JsonSerializerOptions jsonOptions = new() { WriteIndented = true };

        public static List<ModpackInfo> ListInstalledModpacks() {
            Directory.CreateDirectory(modpacksPath);
            List<ModpackInfo> modpackdata = [];

            string[] modpacks = Directory.GetDirectories(modpacksPath);
            foreach (string modpack in modpacks) {
                try {
                    string mineloaderFilePath = Path.Combine(modpack, "mineloader-Aditional.json");
                    string jsonFilePath = Path.Combine(modpack, "TLauncherAdditional.json");

                    if (File.Exists(mineloaderFilePath) && File.Exists(jsonFilePath)) {
                        string jsonContent = File.ReadAllText(jsonFilePath);
                        using JsonDocument doc = JsonDocument.Parse(jsonContent);
                        JsonElement root = doc.RootElement;
                        string mineloaderContent = File.ReadAllText(mineloaderFilePath);
                        using JsonDocument mineloaderdoc = JsonDocument.Parse(mineloaderContent);
                        JsonElement mineloaderroot = mineloaderdoc.RootElement;

                        ModpackInfo parsedModpacks = ParseModpack(root, mineloaderroot);
                        modpackdata.Add(parsedModpacks);
                    }
                }
                catch (Exception ex) {
                    Console.WriteLine("An error occurred: " + ex.Message);
                }
            }

            return modpackdata;
        }

        public static TLauncherData GetLauncherData(string modpackname) {
            string jsonContent = File.ReadAllText(Path.Combine(modpacksPath, modpackname, "TLauncherAdditional.json"));
            using JsonDocument doc = JsonDocument.Parse(jsonContent);
            JsonElement root = doc.RootElement;
            TLauncherData launcherData = ParseTLauncherData(root);
            return launcherData;
        }

        private static ModpackInfo ParseModpack(JsonElement launcerRoot, JsonElement mineloaderRoot) {
            TLauncherData tlauncherData = ParseTLauncherData(launcerRoot);
            MineLoaderData mineLoaderData = ParseMineLoaderData(mineloaderRoot);

            return new ModpackInfo(tlauncherData, mineLoaderData);
        }

        public static TLauncherData ParseTLauncherData(JsonElement launcerRoot) {
            string modpackVersion = launcerRoot.GetProperty("jar").GetString() ?? "Unknown";
            string modpackName = "Unknown";
            List<Mod> mods = [];

            if (launcerRoot.TryGetProperty("modpack", out JsonElement modpackElement)) {
                modpackName = modpackElement.GetProperty("name").GetString() ?? "Unknown";

                if (modpackElement.TryGetProperty("version", out JsonElement versionElement) &&
                    versionElement.TryGetProperty("mods", out JsonElement modsElement)) {
                    mods = ParseTlauncerMods(modsElement);
                }
            }

            return new TLauncherData(modpackName, modpackVersion, mods);
        }

        private static MineLoaderData ParseMineLoaderData(JsonElement mineloaderRoot) {
            return mineloaderRoot.Deserialize<MineLoaderData>() ?? new MineLoaderData();
        }

        private static List<Mod> ParseTlauncerMods(JsonElement modsElement) {
            List<Mod> mods = [];

            foreach (JsonElement mod in modsElement.EnumerateArray()) {
                if (mod.TryGetProperty("stateGameElement", out JsonElement stateElement) &&
                    mod.TryGetProperty("version", out JsonElement modVersionElement) &&
                    modVersionElement.TryGetProperty("metadata", out JsonElement metadataElement) &&
                    metadataElement.TryGetProperty("path", out JsonElement pathElement)) {
                    string modPath = pathElement.GetString() ?? "Unknown";
                    bool isActive = stateElement.GetString() == "active";

                    mods.Add(new Mod { Path = modPath, IsActive = isActive });
                }
            }

            return mods;
        }

        public class ModpackInfo {
            public TLauncherData TLauncher { get; set; }
            public MineLoaderData MineLoader { get; set; }

            public ModpackInfo(TLauncherData tlauncher = null!, MineLoaderData mineLoader = null!) {
                TLauncher = tlauncher ?? new TLauncherData("Default", "Unknown", new List<Mod>());
                MineLoader = mineLoader ?? new MineLoaderData(
                    "Default",
                    new List<Mod>(),
                    new GitHubTree {
                        Sha = "",
                        Url = "",
                        Tree = new List<GitHubTreeItem>(),
                        Truncated = false
                    },
                    false
                );
            }
        }

        public class TLauncherData {
            public string Name { get; set; }
            public string Version { get; set; }
            public List<Mod> Mods { get; set; } = [];

            public TLauncherData(string name, string version, List<Mod> mods) {
                Name = name;
                Version = version;
                Mods = mods;
            }

            public TLauncherData() {
                Name = "Unknown";
                Version = "Unknown";
                Mods = [];
            }
        }

        public class MineLoaderData {
            public int V { get; set; } = 1;
            public string ModpackName { get; set; }
            public List<Mod> Mods { get; set; }
            public GitHubTree FileTree { get; set; }
            public bool AutoUpdate { get; set; }

            public MineLoaderData(string modpackname, List<Mod> mods, GitHubTree tree, bool? update) {
                ModpackName = modpackname;
                Mods = mods;
                FileTree = tree;
                AutoUpdate = update ?? false;
            }

            public MineLoaderData()
            {
                ModpackName = "Unknown";
                Mods = [];
                FileTree = new GitHubTree
                {
                    Sha = "Unknown",
                    Url = "Unknown",
                    Tree = [],
                    Truncated = false
                };
                AutoUpdate = false;
            }
        }
        public class Mod {
            public required string Path { get; set; }
            public bool IsActive { get; set; }
        }

    }
}
