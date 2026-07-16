using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using ModpackInstaller.Infrastructure;

namespace ModpackInstaller.Services;
public static class WebService {
    private static readonly HttpClient client = new();

    public static async Task<string> Get(string url, Dictionary<string, string>? headers = null) {
        var requestMessage = new HttpRequestMessage(HttpMethod.Get, url);

        if (headers != null) {
            foreach (var header in headers) {
                if (!string.IsNullOrEmpty(header.Value)) {
                    requestMessage.Headers.Add(header.Key, header.Value);
                }
            }
        }

        if (!requestMessage.Headers.Contains("User-Agent"))
            requestMessage.Headers.Add("User-Agent", "ModpackInstaller");  // Default User-Agent

        try {
            var response = await client.SendAsync(requestMessage);
            response.EnsureSuccessStatusCode();

            var responseBody = await response.Content.ReadAsStringAsync();
            return responseBody;
        }
        catch (HttpRequestException ex) {
            Console.WriteLine($"Error: {ex.Message}");
            return string.Empty;
        }
    }

    public static async Task<string?> Post(string url, HttpContent content, object? headers = null) {
        try {
            var requestMessage = new HttpRequestMessage(HttpMethod.Post, url) {
                Content = content
            };

            if (headers != null) {
                var properties = headers.GetType().GetProperties();
                foreach (var property in properties) {
                    var key = property.Name;
                    var value = property.GetValue(headers)?.ToString();
                    if (!string.IsNullOrEmpty(value)) {
                        requestMessage.Headers.Add(key, value);
                    }
                }
            }

            HttpResponseMessage response = await client.SendAsync(requestMessage);
            response.EnsureSuccessStatusCode();

            string responseBody = await response.Content.ReadAsStringAsync();
            return responseBody;
        }
        catch (Exception ex) {
            Console.WriteLine($"Error making POST request: {ex.Message}");
            return null;
        }
    }

    public static async Task<T?> GetJson<T>(string url, Dictionary<string, string>? headers = null) {
        var json = await Get(url, headers);

        if (string.IsNullOrEmpty(json)) {
            return default;
        }

        try {
            return JsonSerializer.Deserialize<T>(json, AppVariables.WebJsonOptions);
        }
        catch (JsonException ex) {
            Console.WriteLine($"JSON Parse Error: {ex.Message}");
            return default;
        }
    }

}