using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace ModpackInstaller.Services;
public class Github {
    public static string Token { get; set; } = GetGitToken();
    private static readonly Dictionary<string, string> headers = new() { { "Authorization", "Bearer " + Token } };

    public static async Task<GitHubTree> GetAllRemoteModpacks(string tree = "main") {
        var fileTree = await GetGitHubTreeAsync(false, tree);
        fileTree.Tree = fileTree.Tree
            .Where(i => i.Type == "tree")
            .ToList();
        return fileTree;
    }

    public static async Task<GitHubTree> GetGitHubTreeAsync(bool recursive = true, string tree = "main") {

        string url = recursive
               ? $"https://api.github.com/repos/TheBigSTN/modpacks/git/trees/{tree}?recursive=true"
               : $"https://api.github.com/repos/TheBigSTN/modpacks/git/trees/{tree}";

        if (recursive) {
            if (Cache.Recursive.cachedAt is DateTime cached && Cache.Recursive.GitHubTree is not null) {
                if ((DateTime.UtcNow - cached) < TimeSpan.FromMinutes(5))
                    return Cache.Recursive.GitHubTree;
            }
        }
        else {
            if (Cache.NonRecursive.cachedAt is DateTime cached && Cache.NonRecursive.GitHubTree is not null) {
                if ((DateTime.UtcNow - cached) < TimeSpan.FromMinutes(5))
                    return Cache.NonRecursive.GitHubTree;
            }
        }

        // Fă request
        var response = await WebService.Get(url, headers);
        var treeres = JsonSerializer.Deserialize<GitHubTree>(response)
            ?? throw new Exception("Did not get a tree");

        // Salvează în cache
        if (recursive) {
            Cache.Recursive.GitHubTree = treeres;
            Cache.Recursive.cachedAt = DateTime.UtcNow;
        }
        else {
            Cache.NonRecursive.GitHubTree = treeres;
            Cache.NonRecursive.cachedAt = DateTime.UtcNow;
        }

        return treeres;
    }
    public static async Task DownloadModpack(string modpacksha, string modpackname, bool autoUpdate) {

        GitHubTree gittree = await GetGitHubTreeAsync(true, modpacksha);

        string path = Path.Combine(ModpackService.modpacksPath, modpackname);

        var tasks = new List<Task>();

        foreach (var item in gittree.Tree) {
            if (item.Type == "blob") {
                tasks.Add(WebService.GetFile(item.Url, Path.Combine(path, item.Path), headers));

                if (tasks.Count == 10) {
                    await Task.WhenAll(tasks);
                    tasks.Clear();
                }
            }
            else if (item.Type == "tree") {
                // Create the directory
                Directory.CreateDirectory(Path.Combine(path, item.Path));
            }
        }
        if (tasks.Count > 0) {
            await Task.WhenAll(tasks);
        }

        using JsonDocument launcherData = await GetGitLauncherDataAsync(modpacksha, modpackname);

        JsonSerializerOptions options = new JsonSerializerOptions { WriteIndented = true };

        var data = new ModpackService.MineLoaderData(
            modpackname,
            ModpackService.ParseTLauncherData(launcherData.RootElement).Mods,
            gittree,
            autoUpdate
        );

        using FileStream createStream = File.Create(Path.Combine(ModpackService.modpacksPath, modpackname, "mineloader-Aditional.json"));
        await JsonSerializer.SerializeAsync(createStream, data, options);
        await createStream.FlushAsync();
    }
    public static async Task<GitHubTree> GetModpackWithName(string modpack_name) {
        GitHubTree fullTree = await GetGitHubTreeAsync(false); // arborele principal, non-recursiv

        var modpack = fullTree.Tree.Find(i =>
            i.Type == "tree" && i.Path.Equals(modpack_name, StringComparison.OrdinalIgnoreCase));

        if (modpack == null) {
            Console.WriteLine($"ModpackInfo '{modpack_name}' not found in GitHub tree.");
            return new GitHubTree {
                Sha = string.Empty,
                Url = string.Empty,
                Tree = []
            };
        }

        GitHubTree modpackTree = await GetGitHubTreeAsync(true, modpack.Sha); // recursiv în folderul respectiv
        return modpackTree;
    }
    public static async Task<JsonDocument> GetGitLauncherDataAsync(string modpackSha, string modpackName) {
        var headers = new Dictionary<string, string> {
                { "Authorization", "Bearer " + Token }
            };

        GitHubTree tree = await GetGitHubTreeAsync(true, modpackSha);

        var tlauncherFile = tree.Tree.FirstOrDefault(item =>
            item.Path.Equals("TLauncherAdditional.json", StringComparison.OrdinalIgnoreCase));

        if (tlauncherFile == null)
            throw new Exception("The tlauncer file does not exist on remote");

        string jsonResponse = await WebService.Get(tlauncherFile.Url, headers);

        var jsonDocument = JsonDocument.Parse(jsonResponse);
        if (jsonDocument.RootElement.TryGetProperty("content", out var contentElement)) {
            string? base64Content = contentElement.GetString();
            if (!string.IsNullOrEmpty(base64Content)) {
                // 🔧 Elimină newline-urile (\n) din Base64
                string cleanBase64 = base64Content.Replace("\n", "").Replace("\r", "");

                // Decodează conținutul Base64
                byte[] decodedBytes = Convert.FromBase64String(cleanBase64);

                // Decodeaza bytes în string JSON
                string decodedJson = Encoding.UTF8.GetString(decodedBytes);

                // Parseaza ca JsonDocument
                var tlauncherDoc = JsonDocument.Parse(decodedJson);

                return tlauncherDoc;
            }
            else {
                throw new Exception("The 'content' field is empty.");
            }
        }
        else {
            throw new Exception("The 'content' field was not found in the blob JSON.");
        }
    }
    private static string GetGitToken() {
        var attributes = Assembly
            .GetExecutingAssembly()
            .GetCustomAttributes<AssemblyMetadataAttribute>();

        foreach (var attr in attributes) {
            if (attr.Key == "GitToken" && !string.IsNullOrEmpty(attr.Value))
                return attr.Value;
        }

        var envToken = Environment.GetEnvironmentVariable("GitToken");
        if (!string.IsNullOrEmpty(envToken))
            return envToken;

        throw new Exception("No token found");
    }
    public static void InvalidateCache() {
        Cache.Recursive.GitHubTree = null;
        Cache.Recursive.cachedAt = null;

        Cache.NonRecursive.GitHubTree = null;
        Cache.NonRecursive.cachedAt = null;
    }
    private static class Cache {

        public static class Recursive {
            public static GitHubTree? GitHubTree { get; set; }

            public static DateTime? cachedAt;

        }
        public static class NonRecursive {
            public static GitHubTree? GitHubTree { get; set; }

            public static DateTime? cachedAt;

        }
    }
}

public class GitHubTree {
    [JsonPropertyName("sha")]
    public required string Sha { get; set; }

    [JsonPropertyName("url")]
    public required string Url { get; set; }
    [JsonPropertyName("tree")]

    public required List<GitHubTreeItem> Tree { get; set; }
    [JsonPropertyName("truncated")]
    public bool Truncated { get; set; }
}
public class GitHubTreeItem {
    [JsonPropertyName("path")]
    public required string Path { get; set; }

    [JsonPropertyName("mode")]
    public required string Mode { get; set; }

    [JsonPropertyName("type")]
    public required string Type { get; set; }// can only be tree or blob

    [JsonPropertyName("size")]
    public int Size { get; set; }

    [JsonPropertyName("sha")]
    public required string Sha { get; set; }

    [JsonPropertyName("url")]
    public required string Url { get; set; }
}

