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
using ModpackInstaller.Services.FileSistem;

namespace ModpackInstaller.Services.Modpack;

public class ModpackPublicizeService(ModpackMetadata metadata) {
	private readonly ModpackMetadata _metadata = metadata;
    private readonly ModpackMedatataService _modpackMedatataService = new();

    // =========================================
    // 1. ÎNREGISTRARE UTILIZATOR
    // =========================================
    private static async Task<FullUserDto?> RegisterUserAsync(string nickname) {
        var response = await BackendApiService.RegisterAsync(nickname);
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
        var ownerToken = await GetValidToken();

        var request = CreateRequest(isPublic, sharingCode);
        var response = await BackendApiService.CreateModpackAsync(request, ownerToken);

        if (response != null) {
            // _modpackMedatataService.Update(_metadata.Id, modpackMetadata => {
            //     modpackMetadata.ModpackId = response.Id;
            //     modpackMetadata.ModpackPassword = response.Password;
            //     modpackMetadata.SharingCode = response.ShareCode;
            // });
            _metadata.ModpackId = response.Id;
            _metadata.ModpackPassword = response.Password;
            _metadata.SharingCode = response.ShareCode;

            _modpackMedatataService.Save(_metadata);
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
            var ownerToken = await GetValidToken();
            
            if(_metadata.ModpackId is null || string.IsNullOrEmpty(_metadata.ModpackPassword))
                throw new Exception("Modpack not published.");

            var version = await BackendApiService.InitiateVersionUploadAsync(
                new InitiateVersionUploadRequest(
                    _metadata.ModpackId.Value,
                    semver,
                    versionName
                ),
                ownerToken
            );

            if (version == null)
                return false;
            
            var tree = BuildTree(root);
            
            var missingFiles = await BackendApiService.UploadTreeJsonAsync(
                version.ModpackId,
                version.Id,
                tree,
                ownerToken
            );

            if (missingFiles == null)
                return false;

            foreach (var fullPath in missingFiles.MissingFiles
                         .Select(file => Path.Combine(_metadata.InstallPath, file.FilePath))
                     ) {
                await BackendApiService.UploadFileToBlobStorageAsync(
                    fullPath,
                    ownerToken
                );
            }
            
            await BackendApiService.UpdateVersionStatusAsync(
                new UpdateVersionStatusRequest(
                    Guid.Parse(version.ModpackId),
                    version.Id,
                    ModpackVersionStatus.Private
                ),
                ownerToken
            );

            _metadata.VersionId = version.Id;
            _metadata.VersionSemver = version.Semver;
            _modpackMedatataService.Save(_metadata);

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
            _ = await RegisterUserAsync("TODO");
            token = await GetValidToken(recursion + 1);
        } else if (recursion > 10) {
            throw new Exception("Maximum recursion depth reached");
        }
        return token;
    }

    private CreateModpackRequest CreateRequest(bool isPublic, string? code) {
        return new CreateModpackRequest(
            _metadata.Id,
            _metadata.Name,
            _metadata.Description ?? "",
            _metadata.GameVersion,
            _metadata.Loader.ToString(),
            _metadata.LoaderVersion,
            code ?? _metadata.SharingCode ?? "",
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
                _metadata.InstallPath,
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
