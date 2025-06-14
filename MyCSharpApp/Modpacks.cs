using System.Diagnostics;
using System.IO;
using System.Text.Json;
using Github;

namespace MyCSharpApp {
    public static class Modpacks {
        public static readonly string modpacksPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), ".minecraft", "versions");

        public static List<Modpack> GetModpacks() {
            Directory.CreateDirectory(modpacksPath);
            List<Modpack> modpackdata = [];

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

                            Modpack parsedModpacks = ParseModpack(root, mineloaderroot);
                            modpackdata.Add(parsedModpacks);
                        }
                    }
                    catch (Exception ex) {
                        Console.WriteLine("An error occurred: " + ex.Message);
                    }
                }

            return modpackdata;
        }

        public static Modpack GetModpack(string name) {
            Modpack modpackdata = new();
            try {
                string modpackpath = Path.Combine(modpacksPath, name);
                string jsonFilePath = Path.Combine(modpackpath, "TLauncherAdditional.json");

                if (File.Exists(jsonFilePath)) {
                    string jsonContent = File.ReadAllText(jsonFilePath);
                    using JsonDocument doc = JsonDocument.Parse(jsonContent);
                    JsonElement root = doc.RootElement; 
                    TLauncherData launcherData = ParseTLauncherData(root);
                    modpackdata.TLauncher = launcherData;
                }
                string mineloaderFilePath = Path.Combine(modpackpath, "mineloader-Aditional.json");

                if (File.Exists(mineloaderFilePath)) {
                    string mineloaderContent = File.ReadAllText(mineloaderFilePath);
                    using JsonDocument mineloaderdoc = JsonDocument.Parse(mineloaderContent);
                    JsonElement mineloaderroot = mineloaderdoc.RootElement; 
                    MineLoaderData mineLoaderData = ParseMineLoaderData(mineloaderroot);
                    modpackdata.MineLoader = mineLoaderData;
                }         
            }
            catch (Exception ex) {
                Debug.WriteLine("An error occurred: " + ex.Message);
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

        private static Modpack ParseModpack(JsonElement root, JsonElement mineloaderroot) {
            TLauncherData tlauncherData = ParseTLauncherData(root);
            MineLoaderData mineLoaderData = ParseMineLoaderData(mineloaderroot);

            return new Modpack(tlauncherData, mineLoaderData);
        }

        public static TLauncherData ParseTLauncherData(JsonElement root) {
            string modpackVersion = root.GetProperty("jar").GetString() ?? "Unknown";
            string modpackName = "Unknown";
            List<Mod> mods = [];

            if (root.TryGetProperty("modpack", out JsonElement modpackElement)) {
                modpackName = modpackElement.GetProperty("name").GetString() ?? "Unknown";

                if (modpackElement.TryGetProperty("version", out JsonElement versionElement) &&
                    versionElement.TryGetProperty("mods", out JsonElement modsElement)) {
                    mods = ParseTlauncerMods(modsElement);
                }
            }

            return new TLauncherData(modpackName, modpackVersion, mods);
        }

        private static MineLoaderData ParseMineLoaderData(JsonElement mineloaderroot) {
            return mineloaderroot.Deserialize<MineLoaderData>();
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

        public class Modpack {
            public TLauncherData TLauncher { get; set; }
            public MineLoaderData MineLoader { get; set; }

            public Modpack(TLauncherData tlauncher = null!, MineLoaderData mineLoader = null!) {
                TLauncher = tlauncher ?? new TLauncherData("Default", "Unknown", new List<Mod>());
                MineLoader = mineLoader ?? new MineLoaderData(
                    "Default",
                    new List<Mod>(),
                    new GitHubTree {
                        Sha = "",
                        Url = "",
                        Tree = new List<GitHubTreeItem>(),
                        Truncated = false
                    }
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
        }

        public class MineLoaderData {
            public string ModpackName { get; set; }
            public List<Mod> Mods { get; set; }
            public GitHubTree Tree { get; set; }

            public MineLoaderData(string modpackname, List<Mod> mods, GitHubTree tree) {
                ModpackName = modpackname;
                Mods = mods;
                Tree = tree;
            }
        }
        public class Mod {
            public required string Path { get; set; }
            public bool IsActive { get; set; }
        }

    }
}
