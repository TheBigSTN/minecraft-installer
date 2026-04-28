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
public class WebService {
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
            requestMessage.Headers.Add("User-Agent", "MyCSharpApp");  // Default User-Agent

        try {
            HttpResponseMessage response = await client.SendAsync(requestMessage);
            response.EnsureSuccessStatusCode();

            string responseBody = await response.Content.ReadAsStringAsync();
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

    public static async Task GetFile(string url, string filepath, Dictionary<string, string>? headers) {
        HttpRequestMessage request = new(HttpMethod.Get, url);

        // Add headers to the request
        if (headers != null) {
            foreach (var header in headers) {
                if (!string.IsNullOrEmpty(header.Value)) {
                    request.Headers.Add(header.Key, header.Value);
                }
            }
        }

        request.Headers.Add("User-Agent", "MyCSharpApp");

        HttpResponseMessage? response = null;

        try {
            // Send the request and get the response
            response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
            response.EnsureSuccessStatusCode();

            // Read the response content as a string
            string jsonResponse = await response.Content.ReadAsStringAsync();

            // Parse the JSON response to get the base64 content
            var jsonDocument = JsonDocument.Parse(jsonResponse);
            if (jsonDocument.RootElement.TryGetProperty("content", out var contentElement)) {
                string? base64Content = contentElement.GetString();
                if (!string.IsNullOrEmpty(base64Content)) {
                    // Decode the base64 content
                    byte[] decodedBytes = Convert.FromBase64String(base64Content);

                    // Write the decoded content to the file
                    await File.WriteAllBytesAsync(filepath, decodedBytes);

                    // Console.WriteLine($"File downloaded and decoded successfully to {filepath}");
                }
                else {
                    Console.WriteLine("The content field is empty or null.");
                }
            }
            else {
                Console.WriteLine("The content field was not found in the JSON response.");
            }
        }
        catch (Exception ex) {
            Console.WriteLine($"An error occurred: {ex.Message}");
        }
        finally {
            // Dispose of resources
            response?.Dispose();
        }
    }

    public static async Task<T?> GetJson<T>(string url, Dictionary<string, string>? headers = null) {
        string json = await Get(url, headers);

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