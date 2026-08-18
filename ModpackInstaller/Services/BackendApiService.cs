using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using ModpackInstaller.Infrastructure;
using ModpackInstaller.Models;
using ModpackInstaller.Models.DTOs;
using ModpackInstaller.Models.Backend;
using ModpackInstaller.Services.Modpack;

namespace ModpackInstaller.Services;

public record ServerError(string Message, int Status, DateTime Timestamp);

public static class BackendApiService {
    private static readonly string BaseUrl = AppVariables.AppApiBaseUrl;
    private static readonly HttpClient HttpClient = new();

    static BackendApiService() {
        // Setăm header-ul global pentru toate request-urile viitoare
        HttpClient.DefaultRequestHeaders.Add("User-Agent", "ModpackInstaller");
        
        // Opțional: poți seta și timeout-ul sau alte setări de bază
        HttpClient.Timeout = TimeSpan.FromMinutes(5);
        
        HttpClient.BaseAddress = new Uri(BaseUrl);
    }

    /// <summary>
    /// This usually is called before you upload your first modpack on the backend
    /// This registers you there giving you an Id and a password
    /// </summary>
    /// <param name="nickname"> The username to register with</param>
    /// <returns>
    /// The full user dto if successful
    /// </returns>
    public static async Task<FullUserDto?> RegisterAsync(string nickname) {
        var result = await WebService.Post<FullUserDto>(
            $"{BaseUrl}api/v1/modpacks/register?username={Uri.EscapeDataString(nickname)}");

        if (!result.Success)
            throw result.Exception ?? new Exception(result.RawBody);

        return result.Data;
    }

    // // =========================================
    // // MY LIBRARY (GET /api/v1/modpacks/my-library)
    // // =========================================
    // public static async Task<List<ModpackMetadata>?> GetMyLibraryAsync(string ownerToken) {
    //     using var client = new HttpClient();
    //     client.DefaultRequestHeaders.Add("X-Owner-Token", ownerToken);
    //
    //     var response = await client.GetAsync($"{BaseUrl}api/v1/modpacks/my-library");
    //
    //     if (!response.IsSuccessStatusCode) throw new Exception(await GetErrorMessage(response));
    //
    //     var json = await response.Content.ReadAsStringAsync();
    //     return JsonSerializer.Deserialize<List<ModpackMetadata>>(json, AppVariables.WebJsonOptions);
    // }

    /// <summary>
    /// This initializes a modpack on the backend.
    /// This is required before you can create versions for a modpack / before sharing the modpack
    /// </summary>
    /// <param name="dto">
    /// This is the modpack information
    /// </param>
    /// <param name="ownerToken"> This is your token, this is how you define who is the owner of the modpack</param>
    /// <returns>The full ModpackDto with the password that would usually not get included</returns>
    /// <exception cref="Exception">If the request is non-successful</exception>
    public static async Task<ModpackDto?> CreateModpackAsync(CreateModpackRequest dto, string ownerToken) {
        var result = await WebService.Post<ModpackDto>(
            $"{BaseUrl}api/v1/modpacks",
            dto,
            new Dictionary<string, string> {
                ["X-Owner-Token"] = ownerToken
            });

        if (!result.Success)
            throw result.Exception ?? new Exception(result.RawBody);

        return result.Data;
    }

    // // =========================================
    // // 2. UPDATE METADATA (PUT /api/v1/modpacks/{id})
    // // =========================================
    // public static async Task<ModpackMetadata?> UpdateMetadataAsync(string modpackId, CreateModpackRequest dto, string ownerToken, string modpackPassword) {
    //     using var client = new HttpClient();
    //     client.DefaultRequestHeaders.Add("X-Owner-Token", ownerToken);
    //     client.DefaultRequestHeaders.Add("X-Modpack-Password", modpackPassword);
    //      this has to be reworked
    //     var json = JsonSerializer.Serialize(dto, AppVariables.WebJsonOptions);
    //     var content = new StringContent(json, Encoding.UTF8, "application/json");
    //
    //     var response = await client.PutAsync($"{BaseUrl}api/v1/modpacks/{modpackId}", content);
    //
    //     if (!response.IsSuccessStatusCode) throw new Exception(await GetErrorMessage(response));
    //
    //     var responseJson = await response.Content.ReadAsStringAsync();
    //     return JsonSerializer.Deserialize<ModpackMetadata>(responseJson, AppVariables.WebJsonOptions);
    // }

    /// <summary>
    /// This gets the modpack information from the backend.
    /// This is recommended in some cases instead of the local information
    /// </summary>
    /// <param name="modpackId">The modpack to get</param>
    /// <param name="modpackShareCode">If the modpack is unlisted this is required</param>
    /// <returns>The modpackDto</returns>
    public static async Task<ModpackDto?> GetModpack(Guid modpackId, string? modpackShareCode = null) {
        var result = await WebService.Get<ModpackDto>(
            $"{BaseUrl}api/v1/modpacks/{modpackId}?code={modpackShareCode}");

        if (result.StatusCode == HttpStatusCode.NotFound)
            return null;

        if (!result.Success)
            throw result.Exception ?? new Exception(result.RawBody);

        return result.Data;
    }

    /// <summary>
    /// This is called before you start uploading files or the version tree
    /// </summary>
    /// <param name="requestDto">The version information</param>
    /// <param name="ownerToken">The token of the owner (The owner password)</param>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    public static async Task<ModpackVersionDto?> InitiateVersionUploadAsync(
        InitiateVersionUploadRequest requestDto,
        string ownerToken)
    {
        var result = await WebService.Post<ModpackVersionDto>(
            $"{BaseUrl}api/v1/modpacks/{requestDto.ModpackId}/version/initiate",
            requestDto,
            new Dictionary<string, string> {
                ["X-Owner-Token"] = ownerToken
            });

        result.EnsureSuccess();

        return result.Data;
    }
    
    // =========================================
    // UPLOAD TREE JSON (POST /api/v1/modpacks/{modpackId}/version/{versionId}/tree)
    // =========================================
    public static async Task<MissingFilesResponseDto?> UploadTreeJsonAsync(
        string modpackId,
        Guid versionId,
        ModpackTreeDto tree,
        string ownerToken) {
        var result = await WebService.Post<MissingFilesResponseDto>(
            $"{BaseUrl}api/v1/modpacks/{modpackId}/version/{versionId}/tree",
            tree,
            new Dictionary<string, string> {
                ["X-Owner-Token"] = ownerToken
            });

        result.EnsureSuccess();

        return result.Data;
    }
    
    // =========================================
    // UPLOAD FILE TO BLOB STORAGE (POST /api/v1/files/upload)
    // Returns SHA256 of the uploaded file
    // =========================================
    public static async Task UploadFileToBlobStorageAsync(
        string fullPath,
        string ownerToken
        ) {
        using var form = new MultipartFormDataContent();

        await using var stream = File.OpenRead(fullPath);

        var fileContent = new StreamContent(stream);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");

        form.Add(fileContent, "file", Path.GetFileName(fullPath));

        var result = await WebService.Post<string>(
            $"{BaseUrl}api/v1/files/upload",
            form,
            new Dictionary<string, string>
            {
                ["X-Owner-Token"] = ownerToken
            });

        result.EnsureSuccess();
    }
    
    // =========================================
    // UPDATE VERSION STATUS (PUT /api/v1/modpacks/{modpackId}/version/{versionId}/status)
    // =========================================
    public static async Task UpdateVersionStatusAsync(
        UpdateVersionStatusRequest requestDto, // Using DTO from ModpackInstaller.Models.DTOs
        string ownerToken)
    {
        var json = JsonSerializer.Serialize(requestDto, AppVariables.WebJsonOptions);

        using var request = new HttpRequestMessage(
            HttpMethod.Put,
            $"{BaseUrl}api/v1/modpacks/{requestDto.ModpackId}/version/{requestDto.VersionId}/status"); // Corrected to use VersionId

        request.Headers.Add("X-Owner-Token", ownerToken);
        request.Content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await HttpClient.SendAsync(request);

        if (!response.IsSuccessStatusCode)
            throw new Exception(await GetErrorMessage(response));
    }
    
    // =========================================
    // GET MODPACK MANIFEST (GET /api/v1/modpacks/version/{versionId}/manifest)
    // =========================================
    public static async Task<ModpackManifest?> GetModpackManifestAsync(Guid versionId, string? sharingCode = null) {
        // Construct the URL based on the backend route
        var url = $"{BaseUrl}api/v1/modpacks/notRead/version/{versionId}/manifest";

        if (!string.IsNullOrEmpty(sharingCode))
            url += $"?code={Uri.EscapeDataString(sharingCode)}";

        var result = await WebService.Get<ModpackManifest>(url);

        if (result.StatusCode == HttpStatusCode.NotFound)
            return null;

        result.EnsureSuccess();

        return result.Data;
    }

    // =========================================
    // GET MODPACK VERSION TREE (GET /api/v1/modpacks/{modpackId}/version/{versionId}/tree)
    // =========================================
    public static async Task<ModpackTreeDto?> GetVersionTreeAsync(
        Guid versionId
        ) {
        var result = await WebService.Get<ModpackTreeDto>(
            $"{BaseUrl}api/v1/modpacks/thisisnotused/version/{versionId}/tree");

        result.EnsureSuccess();

        return result.Data;
    }

    // Helper pentru parsarea erorilor de la GlobalExceptionHandler
    private static async Task<string> GetErrorMessage(HttpResponseMessage response) {
        var content = await response.Content.ReadAsStringAsync();
        try {
            var error = JsonSerializer.Deserialize<ServerError>(content, AppVariables.WebJsonOptions);
            return error?.Message ?? $"Server Error {response.StatusCode}";
        }
        catch {
            return content; // Returnăm textul brut dacă nu e JSON valid
        }
    }

    // =========================================
    // GET PUBLIC MODPACKS
    // (GET /api/v1/modpacks/public)
    // =========================================
    public static async Task<List<PublicModpackRequestResponse>> GetPublicModpacksAsync() {
        return await WebService.GetJson<List<PublicModpackRequestResponse>>(
            $"{BaseUrl}api/v1/modpacks/public"
        ) ?? [];
    }
    
    public static async Task<List<ModpackVersionDto>> GetModpackVersionsAsync(Guid modpackId) {
        var headers = new Dictionary<string, string>();

        if (AppSettings.Settings.Config.UserPasswordToken is not null)
            headers["X-Owner-Token"] = AppSettings.Settings.Config.UserPasswordToken;

        return await WebService.GetJson<List<ModpackVersionDto>>(
            $"{BaseUrl}api/v1/modpacks/{modpackId}/version",
            headers: headers
        ) ?? [];
    }

    // =========================================
    // DOWNLOAD FULL VERSION (GET /api/v1/modpacks/{modpackId}/version/{versionId})
    // =========================================
    public static async Task DownloadVersionAsync(
        string modpackId,
        Guid versionId,
        string savePath,
        string? sharingCode = null,
        IProgress<double>? progress = null
    ) {
        var url =
            $"{BaseUrl}api/v1/modpacks/{modpackId}/version/{versionId}";

        if (!string.IsNullOrEmpty(sharingCode))
        {
            url += $"?code={Uri.EscapeDataString(sharingCode)}";
        }

        await DownloadRawFile(url, savePath, progress);
    }
    
    /// =========================================
    /// DOWNLOAD STORED FILE (GET /api/v1/files/{sha256})
    /// =========================================
    public static Task DownloadStoredFileAsync(
        string sha256,
        string savePath,
        IProgress<double>? progress = null
        ) {
        return DownloadRawFile(
            $"{BaseUrl}api/v1/files/{sha256}",
            savePath,
            progress);
    }

    // =========================
    // INTERNAL RAW FILE DOWNLOAD
    // (folosit pt zip-uri reale, nu base64)
    // =========================
    private static async Task DownloadRawFile( string url, string filepath, IProgress<double>? progress = null ) {

        using var response = await HttpClient.GetAsync(
            url,
            HttpCompletionOption.ResponseHeadersRead
        );

        response.EnsureSuccessStatusCode();

        var totalBytes = response.Content.Headers.ContentLength;

        await using var stream = await response.Content.ReadAsStreamAsync();
        await using var file = File.Create(filepath);

        var buffer = new byte[8192];
        long totalReadBytes = 0;
        int readBytes;

        while((readBytes = await stream.ReadAsync(buffer)) > 0) {
            await file.WriteAsync(buffer.AsMemory(0, readBytes));
            totalReadBytes += readBytes;

            if (!totalBytes.HasValue) 
                continue;
            
            var progressPercentage = (double)totalReadBytes / totalBytes.Value * 100;
            progress?.Report(progressPercentage);
        }

        progress?.Report(100);
    }
}