using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ModpackInstaller.Infrastructure;
using ModpackInstaller.Models;
using ModpackInstaller.Models.Backend;
using ModpackInstaller.Models.DTOs;
using ModpackInstaller.Models.FileSistem;
using ModpackInstaller.Models.Interfaces;
using ModpackInstaller.Services.FileSistem;

namespace ModpackInstaller.Services.Modpack;

public class ModpackPublicizeService(ModpackMetadataStorage metadataStorage) {
    private readonly ModpackMetadataStorage _metadataStorage = metadataStorage;

    // =========================================
    // 1. ÎNREGISTRARE UTILIZATOR
    // =========================================
    private static async Task<FullUserDto?> RegisterUserAsync(string nickname) {
        var response = await BackendApiService.RegisterAsync(nickname).ConfigureAwait(false);
        if (response != null) {
            AppSettings.Settings.Update(cfg => {
                cfg.UserPasswordToken = response.Token;
                cfg.UserName = response.Username;
                cfg.UserId = response.Id;
            });
        }

        return response;
    }

    // =========================================
    // 2. CREARE MODPACK (INIȚIALIZARE)
    // =========================================
    public async Task CreateOnServerAsync(bool isPublic, string? sharingCode = null) {
        var ownerToken = await GetValidToken().ConfigureAwait(false);

        var request = CreateRequest(isPublic, sharingCode);
        var response = await BackendApiService.CreateModpackAsync(request, ownerToken).ConfigureAwait(false);

        if (response != null) {
            _metadataStorage.Update(modpackMetadata => {
                modpackMetadata.ModpackId = response.Id;
                modpackMetadata.ModpackPassword = response.Password;
                modpackMetadata.SharingCode = response.ShareCode;
            });
        }
    }

    // =========================================
    // 4. UPLOAD VERSIUNE NOUĂ
    // =========================================
    public async Task<bool> UploadNewVersionAsync(
        TreeNode root,
        string semver,
        string versionName
    ) {
        try {
            var ownerToken = await GetValidToken().ConfigureAwait(false);

            if (_metadataStorage.IsPublished)
                throw new Exception("Modpack not published.");

            var version = await BackendApiService.InitiateVersionUploadAsync(
                new InitiateVersionUploadRequest(
                    _metadataStorage.GetData().ModpackId!.Value, // IsPublished ensures it's not null
                    semver,
                    versionName
                ),
                ownerToken
            ).ConfigureAwait(false);

            if (version == null)
                return false;

            var tree = BuildTree(root);

            var missingFiles = await BackendApiService.UploadTreeJsonAsync(
                version.ModpackId,
                version.Id,
                tree,
                ownerToken
            ).ConfigureAwait(false);

            if (missingFiles == null)
                return false;

            foreach (var fullPath in missingFiles.MissingFiles
                         .Select(file => Path.Combine(_metadataStorage.GetData().InstallPath, file.FilePath))
                    ) {
                await BackendApiService.UploadFileToBlobStorageAsync(
                    fullPath,
                    ownerToken
                ).ConfigureAwait(false);
            }

            await BackendApiService.UpdateVersionStatusAsync(
                new UpdateVersionStatusRequest(
                    Guid.Parse(version.ModpackId),
                    version.Id,
                    ModpackVersionStatus.Private
                ),
                ownerToken
            ).ConfigureAwait(false);

            _metadataStorage.Update(local => {
                local.VersionId = version.Id;
                local.VersionSemver = version.Semver;
            });

            return true;
        }
        catch (Exception ex) {
            CrashReporter.Log(ex, nameof(UploadNewVersionAsync));
            return false;
        }
    }

    private static List<string> GetExcludedFiles(TreeNode root) {
        var excludedFiles = new List<string>();

        if (root.IsFile) {
            var relativePath = root.Node.RelativePath;

            if (!root.IsChecked)
                excludedFiles.Add(relativePath);
        }

        foreach (var child in root.Children)
            GetExcludedFiles(child);

        return excludedFiles;
    }

    // =========================================
    // HELPERS
    // =========================================

    private static async Task<string> GetValidToken(int recursion = 0) {
        var token = AppSettings.Settings.Config.UserPasswordToken;
        if (string.IsNullOrEmpty(token)) {
            _ = await RegisterUserAsync("TODO").ConfigureAwait(false);
            token = await GetValidToken(recursion + 1).ConfigureAwait(false);
        }
        else if (recursion > 10) {
            throw new Exception("Maximum recursion depth reached");
        }

        return token;
    }

    private CreateModpackRequest CreateRequest(bool isPublic, string? code) {
        var data = _metadataStorage.GetData();
        
        return new CreateModpackRequest(
            data.Id,
            data.Name,
            data.Description ?? "",
            data.GameVersion,
            data.Loader.ToString(),
            data.LoaderVersion,
            code ?? data.SharingCode ?? "",
            isPublic
        );
    }

    public static string GenerateCode(int length = 10) {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        var random = new Random();
        return new string(Enumerable.Range(0, length)
            .Select(_ => chars[random.Next(chars.Length)])
            .ToArray());
    }


    private ModpackTreeDto BuildTree(TreeNode root) {
        List<ModpackFileDto> files = [];

        CollectFiles(root, files);

        return new ModpackTreeDto(files);
    }

    private void CollectFiles(
        TreeNode node,
        List<ModpackFileDto> files) {
        if (node is { IsFile: true, IsChecked: true }) {
            var fullPath = Path.Combine(
                _metadataStorage.GetData().InstallPath,
                node.Node.RelativePath);

            files.Add(new ModpackFileDto(
                node.Node.RelativePath.Replace('\\', '/'),
                FileHasher.ComputeSha256(fullPath),
                new FileInfo(fullPath).Length
            ));
        }

        foreach (var child in node.Children)
            CollectFiles(child, files);
    }
}