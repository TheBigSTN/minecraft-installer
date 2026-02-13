using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModpackInstaller.Services;

using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using ModpackInstaller.Infrastructure;
using ModpackInstaller.Models;

public record OwnerResponse(string OwnerToken, string Nickname);

public record ModpackRequest(
    string Id,
    string Name,
    string Description,
    string GameVersion,
    string Loader,
    string LoaderVersion,
    string SharingCode,
    bool IsPublic
);

public class ModpackResponse : ModpackMetadata;

public record ServerError(string Message, int Status, DateTime Timestamp);

public static class ModpackApiService {
    private static readonly string _baseUrl = AppVariables.AppApiBaseUrl;

    // =========================================
    // 0. REGISTER (POST /api/v1/modpacks/register)
    // =========================================
    public static async Task<OwnerResponse?> RegisterAsync(string nickname) {
        using var client = new HttpClient();
        // Parametrul vine în URL ca Query Param conform semnăturii tale Java
        var response = await client.PostAsync($"{_baseUrl}/api/v1/modpacks/register?nickname={Uri.EscapeDataString(nickname)}", null);

        if (!response.IsSuccessStatusCode) throw new Exception(await GetErrorMessage(response));

        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<OwnerResponse>(json, AppVariables.WebJsonOptions);
    }

    // =========================================
    // MY LIBRARY (GET /api/v1/modpacks/my-library)
    // =========================================
    public static async Task<List<ModpackResponse>?> GetMyLibraryAsync(string ownerToken) {
        using var client = new HttpClient();
        client.DefaultRequestHeaders.Add("X-Owner-Token", ownerToken);

        var response = await client.GetAsync($"{_baseUrl}/api/v1/modpacks/my-library");

        if (!response.IsSuccessStatusCode) throw new Exception(await GetErrorMessage(response));

        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<List<ModpackResponse>>(json, AppVariables.WebJsonOptions);
    }

    // =========================================
    // Create Modpack /api/v1/modpacks)
    // =========================================
    public static async Task<ModpackResponse?> CreateModpackAsync(ModpackRequest dto, string ownerToken) {
        using var client = new HttpClient();
        client.DefaultRequestHeaders.Add("X-Owner-Token", ownerToken);

        var json = JsonSerializer.Serialize(dto, AppVariables.WebJsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await client.PostAsync($"{_baseUrl}/api/v1/modpacks", content);

        if (!response.IsSuccessStatusCode) throw new Exception(await GetErrorMessage(response));

        var responseJson = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<ModpackResponse>(responseJson, AppVariables.WebJsonOptions);
    }

    // =========================================
    // 2. UPDATE METADATA (PUT /api/v1/modpacks/{id})
    // =========================================
    public static async Task<ModpackResponse?> UpdateMetadataAsync(string modpackId, ModpackRequest dto, string ownerToken, string modpackPassword) {
        using var client = new HttpClient();
        client.DefaultRequestHeaders.Add("X-Owner-Token", ownerToken);
        client.DefaultRequestHeaders.Add("X-Modpack-Password", modpackPassword);

        var json = JsonSerializer.Serialize(dto, AppVariables.WebJsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await client.PutAsync($"{_baseUrl}/api/v1/modpacks/{modpackId}", content);

        if (!response.IsSuccessStatusCode) throw new Exception(await GetErrorMessage(response));

        var responseJson = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<ModpackResponse>(responseJson, AppVariables.WebJsonOptions);
    }


    // =========================================
    // 3. UPLOAD VERSION (POST /api/v1/modpacks/{id}/versions)
    // =========================================
    public static async Task<string> UploadVersionAsync(string modpackId, int version, string zipFilePath, string ownerToken, string modpackPassword) {
        using var client = new HttpClient();
        client.DefaultRequestHeaders.Add("X-Owner-Token", ownerToken);
        client.DefaultRequestHeaders.Add("X-Modpack-Password", modpackPassword);

        using var form = new MultipartFormDataContent();

        // Fișierul ZIP
        var fileStream = File.OpenRead(zipFilePath);
        var fileContent = new StreamContent(fileStream);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/zip");

        // "file" este numele @RequestPart din Java
        form.Add(fileContent, "file", Path.GetFileName(zipFilePath));

        // URL-ul conține variabila de versiune ca @RequestParam
        var response = await client.PostAsync($"{_baseUrl}/api/v1/modpacks/{modpackId}/versions?version={version}", form);

        if (!response.IsSuccessStatusCode) throw new Exception(await GetErrorMessage(response));

        return await response.Content.ReadAsStringAsync();
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





    // =========================
    // DOWNLOAD FULL VERSION
    // =========================
    public static async Task DownloadVersionAsync(
        string modpackId,
        int version,
        string savePath
    ) {
        var url =
            $"{_baseUrl}/api/v1/modpacks/{modpackId}/versions/{version}";

        await DownloadRawFile(url, savePath);
    }

    // =========================
    // DOWNLOAD PATCH FROM vN TO vM
    // =========================
    public static async Task DownloadPatchAsync(
        string modpackId,
        int fromVersion,
        int toVersion,
        string savePath
    ) {
        var url =
            $"{_baseUrl}/api/v1/modpacks/{modpackId}/patch" +
            $"?from={fromVersion}&to={toVersion}";

        await DownloadRawFile(url, savePath);
    }

    // =========================
    // INTERNAL RAW FILE DOWNLOAD
    // (folosit pt zip-uri reale, nu base64)
    // =========================
    private static async Task DownloadRawFile(string url, string filepath) {
        using var client = new HttpClient();
        client.DefaultRequestHeaders.Add("User-Agent", "MyCSharpApp");

        using var response = await client.GetAsync(
            url,
            HttpCompletionOption.ResponseHeadersRead
        );

        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync();
        await using var file = File.Create(filepath);

        await stream.CopyToAsync(file);
    }
}
