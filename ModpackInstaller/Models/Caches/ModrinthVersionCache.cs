using System.Collections.Generic;
using System.Threading.Tasks;
using ModpackInstaller.Models.Modrinth;
using ModpackInstaller.Services;

namespace ModpackInstaller.Models.Caches;

public static class ModrinthVersionCache {
    private static readonly Dictionary<string, Task<ModrinthVersion?>> Cache = new();
    private static readonly object Lock = new();

    public static ModrinthVersion? Get(string key) {
        lock (Lock) {
            if (Cache.TryGetValue(key, out var task))
                return task.IsCompletedSuccessfully
                    ? task.GetAwaiter().GetResult()
                    : null;
            Cache[key] = RequestAsync(key);
        }

        return null;
    }

    public static Task<ModrinthVersion?> GetAsync(string key) {
        lock (Lock) {
            if (Cache.TryGetValue(key, out var task))
                return task;

            task = RequestAsync(key);
            Cache.Add(key, task);

            return task;
        }
    }

    private static Task<ModrinthVersion?> RequestAsync(string key) {
        // Do actual request here
        return ModrinthApiService.GetVersionAsync(key);
    }
}