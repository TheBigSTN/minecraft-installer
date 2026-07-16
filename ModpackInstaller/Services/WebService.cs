using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using ModpackInstaller.Infrastructure;

namespace ModpackInstaller.Services;

public static class WebService {
    public static readonly HttpClient Client = new();

    static WebService() {
        Client.DefaultRequestHeaders.Add("User-Agent", "ModpackInstaller");

        Client.Timeout = TimeSpan.FromMinutes(5);
    }

    public static async Task<HttpResult<T>> Get<T>(string url, Dictionary<string, string>? headers = null) {
        using var request = new HttpRequestMessage(HttpMethod.Get, url);

        if (headers != null) {
            foreach (var header in headers
                         .Where(header => !string.IsNullOrWhiteSpace(header.Value))) {
                request.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }
        }

        try {
            using var response = await Client.SendAsync(request);

            var body = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode) {
                Console.WriteLine("========== HTTP REQUEST FAILED ==========");
                Console.WriteLine($"Time:      {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                Console.WriteLine($"Method:    {request.Method}");
                Console.WriteLine($"Url:       {request.RequestUri}");
                Console.WriteLine($"Status:    {(int)response.StatusCode} ({response.StatusCode})");

                if (request.Headers.Any()) {
                    Console.WriteLine("Headers:");
                    foreach (var header in request.Headers)
                        Console.WriteLine($"  {header.Key}: {string.Join(", ", header.Value)}");
                }

                Console.WriteLine("Response:");
                Console.WriteLine(body);
                Console.WriteLine("=========================================");

                return new HttpResult<T> { Success = false, StatusCode = response.StatusCode, RawBody = body };
            }

            T? data;

            if (typeof(T) == typeof(string)) {
                data = (T)(object)body;
            }
            else if (string.IsNullOrWhiteSpace(body)) {
                data = default;
            }
            else {
                data = JsonSerializer.Deserialize<T>(body, AppVariables.WebJsonOptions);
            }

            return new HttpResult<T> { Success = true, StatusCode = response.StatusCode, Data = data, RawBody = body };
        }
        catch (Exception ex) {
            Console.WriteLine("========== HTTP EXCEPTION ==========");
            Console.WriteLine($"Time:      {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            Console.WriteLine($"Method:    {request.Method}");
            Console.WriteLine($"Url:       {request.RequestUri}");
            Console.WriteLine(ex);
            Console.WriteLine("====================================");

            return new HttpResult<T> { Success = false, Exception = ex, RawBody = string.Empty };
        }
    }

    public static async Task<HttpResult<T>> Post<T>(
        string url,
        HttpContent? content = null,
        Dictionary<string, string>? headers = null) {
        using var request = new HttpRequestMessage(HttpMethod.Post, url);
        request.Content = content;

        if (headers != null) {
            foreach (var header in headers
                         .Where(header => !string.IsNullOrWhiteSpace(header.Value))) {
                request.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }
        }

        try {
            using var response = await Client.SendAsync(request);

            var body = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode) {
                Console.WriteLine("========== HTTP REQUEST FAILED ==========");
                Console.WriteLine($"Time:      {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                Console.WriteLine($"Method:    {request.Method}");
                Console.WriteLine($"Url:       {request.RequestUri}");
                Console.WriteLine($"Status:    {(int)response.StatusCode} ({response.StatusCode})");

                if (request.Headers.Any()) {
                    Console.WriteLine("Headers:");
                    foreach (var header in request.Headers)
                        Console.WriteLine($"  {header.Key}: {string.Join(", ", header.Value)}");
                }

                if (request.Content != null) {
                    Console.WriteLine("Request Body:");
                    Console.WriteLine(await request.Content.ReadAsStringAsync());
                }

                Console.WriteLine("Response:");
                Console.WriteLine(body);
                Console.WriteLine("=========================================");

                return new HttpResult<T> { Success = false, StatusCode = response.StatusCode, RawBody = body };
            }

            T? data;

            if (typeof(T) == typeof(string)) {
                data = (T)(object)body;
            }
            else if (string.IsNullOrWhiteSpace(body)) {
                data = default;
            }
            else {
                data = JsonSerializer.Deserialize<T>(body, AppVariables.WebJsonOptions);
            }

            return new HttpResult<T> { Success = true, StatusCode = response.StatusCode, Data = data, RawBody = body };
        }
        catch (Exception ex) {
            Console.WriteLine("========== HTTP EXCEPTION ==========");
            Console.WriteLine($"Time:      {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            Console.WriteLine($"Method:    {request.Method}");
            Console.WriteLine($"Url:       {request.RequestUri}");

            if (request.Content != null) {
                Console.WriteLine("Request Body:");
                Console.WriteLine(await request.Content.ReadAsStringAsync());
            }

            Console.WriteLine(ex);
            Console.WriteLine("====================================");

            return new HttpResult<T> { Success = false, Exception = ex };
        }
    }

    public static Task<HttpResult<TResponse>> Post<TResponse>(
        string url,
        object body,
        Dictionary<string, string>? headers = null) {
        var json = JsonSerializer.Serialize(body, AppVariables.WebJsonOptions);

        var content = new StringContent(
            json,
            Encoding.UTF8,
            "application/json");

        return Post<TResponse>(url, content, headers);
    }

    public static async Task<T?> GetJson<T>(string url, Dictionary<string, string>? headers = null) {
        var json = await Get<string>(url, headers);

        if (string.IsNullOrEmpty(json.RawBody)) {
            return default;
        }

        try {
            return JsonSerializer.Deserialize<T>(json.RawBody, AppVariables.WebJsonOptions);
        }
        catch (JsonException ex) {
            Console.WriteLine($"JSON Parse Error: {ex.Message}");
            return default;
        }
    }
}

public sealed class HttpResult<T> {
    public bool Success { get; init; }
    public HttpStatusCode StatusCode { get; init; }
    public T? Data { get; init; }
    public string RawBody { get; init; } = string.Empty;
    public Exception? Exception { get; init; }

    public void EnsureSuccess() {
        if (Success)
            return;

        throw Exception ?? new HttpRequestException(
            string.IsNullOrWhiteSpace(RawBody)
                ? $"HTTP request failed with status code {(int)StatusCode} ({StatusCode})."
                : RawBody);
    }
}