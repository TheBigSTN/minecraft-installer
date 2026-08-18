using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using ModpackInstaller.Infrastructure;
using ModpackInstaller.Models;
using ModpackInstaller.Models.Caches;
using ModpackInstaller.Models.Modrinth;
using Environment = ModpackInstaller.Models.Modrinth.Environment;

namespace ModpackInstaller.Services.Modpack;

public class ModFilesystemSyncService(
    ModpackManifestStorage store,
    ModInstallationManager installationManager) {
    public async Task SyncWithFilesystemAsync() {
        var modsFolder = Path.Combine(store.InstallPath, "mods");
        if (!Directory.Exists(modsFolder)) return;

        var jarFiles = Directory.GetFiles(modsFolder, "*.jar");

        foreach (var filePath in jarFiles) {
            var fileName = Path.GetFileName(filePath);

            if (store.InstalledMods.Any(m => m.Filename == fileName)) continue;

            try {
                var sha1 = HashUtils.ComputeSha1(filePath);
                var version = await ModrinthApiService.GetVersionByHashAsync(sha1);

                if (version != null) {
                    var existingMod = store.InstalledMods.FirstOrDefault(m => m.ProjectId == version.ProjectId);
                    if (existingMod != null) {
                        if (existingMod.VersionId == version.Id) continue;

                        try {
                            File.Delete(filePath);
                        }
                        catch (Exception ex) {
                            Console.WriteLine($"Failed to delete {fileName}: {ex.Message}");
                        }

                        continue;
                    }

                    await installationManager.InstallModAsync(new ModVersion(version.ProjectId, version.Id), false);
                }
                else {
                    var (title, versionStr) = ParseFileName(fileName);
                    var mod = new ModInfo {
                        ProjectId = Guid.NewGuid().ToString(),
                        VersionId = versionStr,
                        VersionNumber = versionStr,
                        Title = title,
                        Filename = fileName,
                        Source = ModSource.Local,
                        Enabled = true,
                        OwnerName = "Unknown",
                        Environment = Environment.ClientAndServer
                    };

                    store.InstalledMods.Add(mod);
                }
            }
            catch (Exception ex) {
                Console.WriteLine($"[Sync Error] {fileName}: {ex.Message}");
            }
        }

        store.MarkDirty();
    }

    public async Task SyncToFileSystemAsync(bool serverInstall) {
        var modsFolder = Path.Combine(store.InstallPath, "mods");
        
        // Console.Write(JsonSerializer.Serialize(store.InstalledMods, AppVariables.DefaultJsonOptions));

        foreach (var mod in store.InstalledMods.ToList()) {
            if (mod.Environment is Environment.HaveToRequest) {
                var version = await ModrinthVersionCache.GetAsync(mod.VersionId)
                                                        .ConfigureAwait(false);

                if (version is not null)
                    mod.Environment = version.Environment;
                store.MarkDirty();
            }

            if (!ShouldDownload(mod, serverInstall)) continue;

            var filePath = Path.Combine(modsFolder, mod.Filename);
            var disabledPath = filePath + ".deactivation";

            if (File.Exists(disabledPath)) continue;

            if (!File.Exists(filePath)) {
                mod.Enabled = true;
                await ModInstallationManager.DownloadModAsync(mod, store.InstallPath).ConfigureAwait(false);
                continue;
            }

            if (mod.Source != ModSource.Remote || await IsValidModAsync(mod, filePath).ConfigureAwait(false)) continue;

            File.Delete(filePath);
            await ModInstallationManager.DownloadModAsync(mod, store.InstallPath).ConfigureAwait(false);
        }

        store.MarkDirty();
    }

    private async Task<bool> IsValidModAsync(ModInfo mod, string filePath) {
        if (mod.Source != ModSource.Remote) return true;

        if (!string.IsNullOrWhiteSpace(mod.FileSha)) {
            return await CompareLocalFileAsync(filePath, mod.FileSha).ConfigureAwait(false);
        }

        var version = await ModrinthApiService.GetVersionAsync(mod.VersionId).ConfigureAwait(false);
        if (version == null) return true;

        var file = version.Files.FirstOrDefault(f => f.Filename == mod.Filename) ?? version.PrimaryFile;
        if (file?.Hashes.Sha1 == null) return true;

        mod.FileSha = file.Hashes.Sha1.ToLowerInvariant();
        var index = store.InstalledMods.IndexOf(mod);
        if (index != -1) {
            store.InstalledMods[index].FileSha = mod.FileSha;
        }

        return await CompareLocalFileAsync(filePath, mod.FileSha);
    }

    private static async Task<bool> CompareLocalFileAsync(string filePath, string expectedSha1) {
        await using var stream = new FileStream(
            filePath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            bufferSize: 1024 * 1024,
            options: FileOptions.Asynchronous | FileOptions.SequentialScan
        );

        using var sha1 = System.Security.Cryptography.SHA1.Create();
        var hash = await sha1.ComputeHashAsync(stream);
        var local = Convert.ToHexString(hash).ToLowerInvariant();

        return local == expectedSha1.ToLowerInvariant();
    }

    private static (string title, string version) ParseFileName(string fileName) {
        var name = Path.GetFileNameWithoutExtension(fileName);
        var parts = name.Split('-', StringSplitOptions.RemoveEmptyEntries);

        if (parts.Length == 0) return (name, "");

        var title = parts[0];
        var version = parts.Length > 1 ? parts[^1] : "";

        title = title.Replace("_", " ");
        title = char.ToUpper(title[0]) + title[1..];

        return (title, version);
    }

    private static bool ShouldDownload(ModInfo mod, bool serverInstall) {
        return mod.Environment switch {
            Environment.ClientAndServer => true,
            Environment.ClientOnly => !serverInstall,
            Environment.ClientOnlyServerOptional => true,
            Environment.SingleplayerOnly => !serverInstall,
            Environment.ServerOnly => serverInstall,
            Environment.ServerOnlyClientOptional => true,
            Environment.DedicatedServerOnly => serverInstall,
            Environment.ClientOrServer => true,
            Environment.ClientOrServerPrefersBoth => true,
            Environment.Unknown => false,
            Environment.HaveToRequest =>
                throw new InvalidOperationException(
                    "Mod environment has not been resolved."),
            _ => throw new ArgumentOutOfRangeException()
        };
    }
}