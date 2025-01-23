using System.Text.Json;
using System.Text.Json.Serialization;
using Html.Web;

namespace Github {
    public class GithubHelper {
        private static string Token { get; set; } = Environment.GetEnvironmentVariable("GitToken") ?? throw new Exception("No token found");

        public static async Task<GitHubTree> GetGitHubTreeAsync(bool recursive = true, string tree = "main") {
            var url = recursive
             ? $"https://api.github.com/repos/TheBigSTN/modpacks/git/trees/{tree}?recursive={recursive}"
             : $"https://api.github.com/repos/TheBigSTN/modpacks/git/trees/{tree}";

            var headers = new Dictionary<string, string> {
                { "Authorization", "Bearer " + Token }
            };

            var response = await Exiom.Get(url, headers);

            var treeres = JsonSerializer.Deserialize<GitHubTree>(response)
                ?? throw new Exception("Did not get a tree");

            return treeres;
        }
        public static async Task DownloadModpack(string modpacksha) {
            var headers = new Dictionary<string, string> {
                { "Authorization", "Bearer " + Token }
            };

            GitHubTree gittree = await GetGitHubTreeAsync(true, modpacksha);

            string path = "D:\\Testing\\";

            var tasks = new List<Task>();
            var stopwatch = new System.Diagnostics.Stopwatch();
            stopwatch.Start();

            foreach (var item in gittree.Tree) {
                if (item.Type == "blob") {
                    tasks.Add(Exiom.GetFile(item.Url, path + item.Path, headers));

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

            stopwatch.Stop();
            Console.WriteLine($"Downloaded in {stopwatch.ElapsedMilliseconds}ms");
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
        public required string Type { get; set; }

        [JsonPropertyName("size")]
        public int Size { get; set; }

        [JsonPropertyName("sha")]
        public required string Sha { get; set; }

        [JsonPropertyName("url")]
        public required string Url { get; set; }
    }
}
