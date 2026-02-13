using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using ModpackInstaller.Infrastructure;
using ModpackInstaller.Models;
using ReactiveUI;

namespace ModpackInstaller.Services.Modpack;

public class ModpackPublicizeService(ModpackMetadata metadata) {
	private readonly AppSettings _appSettings = new(AppVariables.InstallerRoot);
	private readonly ModpackMetadata _metadata = metadata;
    private readonly ModpackMedatataService _modpackMedatataService = new(AppVariables.InstallerRoot);

    // =========================================
    // 1. ÎNREGISTRARE UTILIZATOR
    // =========================================
    public async Task<OwnerResponse?> RegisterUserAsync(string nickname) {
        var response = await ModpackApiService.RegisterAsync(nickname);
        if (response != null) {
            _appSettings.Update(cfg => {
                cfg.UserPasswordToken = response.OwnerToken;
                cfg.UserNickname = response.Nickname;
            });
        }
        return response;
    }

    // =========================================
    // 2. CREARE MODPACK (INIȚIALIZARE)
    // =========================================
    public async Task CreateOnServerAsync(bool isPublic, string? sharingCode = null) {
        string ownerToken = await GetValidToken();

        var request = CreateRequest(isPublic, sharingCode);
        var response = await ModpackApiService.CreateModpackAsync(request, ownerToken);

        if (response != null) {
            _metadata.Id = response.Id;
            _metadata.ModpackPassword = response.ModpackPassword;
            _metadata.IsPublic = response.IsPublic;
            _metadata.SharingCode = response.SharingCode;
            _metadata.Author = _metadata.OwnerNickname = response.Author;
            // Aici ar trebui să apelezi salvarea metadata pe disk

            _modpackMedatataService.Save(_metadata);
        }
    }

    // =========================================
    // 3. UPDATE METADATA (DOAR TEXT/SETĂRI)
    // =========================================
    public async Task UpdateMetadataAsync() {
        string ownerToken = await GetValidToken();
        if (string.IsNullOrEmpty(_metadata.Id) || string.IsNullOrEmpty(_metadata.ModpackPassword))
            throw new Exception("Modpack-ul nu a fost creat pe server încă.");

        var request = CreateRequest(_metadata.IsPublic, _metadata.SharingCode);
        await ModpackApiService.UpdateMetadataAsync(_metadata.Id, request, ownerToken, _metadata.ModpackPassword);
    }

    // =========================================
    // 4. UPLOAD VERSIUNE NOUĂ (ZIP)
    // =========================================
    public async Task UploadNewVersionAsync() {
        string ownerToken = await GetValidToken();
        if (string.IsNullOrEmpty(_metadata.Id)) throw new Exception("ID Modpack lipsă.");
        if (string.IsNullOrEmpty(_metadata.ModpackPassword)) throw new Exception("Modpack not Published");

        string zipPath = AppVariables.GetTempFilePath($"{_metadata.Id}_v{_metadata.Version}.zip");

        try {
            // Exportă fișierul fizic
            ModpackPackageService.ExportFullAsync(_metadata.InstallPath, zipPath);

            // Trimite la server
            await ModpackApiService.UploadVersionAsync(
                _metadata.Id,
                _metadata.Version,
                zipPath,
                ownerToken,
                _metadata.ModpackPassword!
            );
        }
        finally {
            if (File.Exists(zipPath)) File.Delete(zipPath);
        }
    }

    // =========================================
    // HELPERS
    // =========================================

    private async Task<string> GetValidToken(int recursion = 0) {
        string? token = _appSettings.Config.UserPasswordToken;
        if (string.IsNullOrEmpty(token)) {
            await RegisterUserAsync("TODO");
            token = await GetValidToken(recursion + 1);
        } else if (recursion > 10) {
            throw new Exception("Maximum recursion depth reached");
        }
        return token;
    }

    private ModpackRequest CreateRequest(bool isPublic, string? code) {
        return new ModpackRequest(
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

    //public static async Task PublicizeAsync(ModpackMetadata metadata, bool ShouldBePublic = true) {
    //	string zipPath = AppVariables.GetTempFilePath(
    //		$"{metadata.Name}_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.zip"
    //	);

    //	try {
    //		// 1️⃣ Creează export temporar
    //		ModpackPackageService.ExportFullAsync(metadata.InstallPath, zipPath);
    //		metadata.IsPublic = ShouldBePublic;

    //		await ModpackApiService.UploadModpackAsync(metadata, zipPath);
    //       }
    //	finally {
    //		// Cleanup
    //		if (File.Exists(zipPath))
    //			File.Delete(zipPath);
    //	}
    //}

    //  public static async Task PublicizeUnlistedAsync(ModpackMetadata metadata, int codeLength = 6) {
    //          string randcode = GenerateCode(codeLength);

    //          metadata.IsPublic = false;
    //          metadata.SharingCode = randcode;

    //await PublicizeAsync(metadata, false);
    //  }

    //   private static string GenerateCode(int length) {
    //	const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
    //	var random = new Random();

    //	return new string(
    //		Enumerable.Range(0, length)
    //				  .Select(_ => chars[random.Next(chars.Length)])
    //				  .ToArray());
    //}
}
