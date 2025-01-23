using System.Text.Json;

namespace Html.Web {
    public class Exiom {
        private static readonly HttpClient client = new();

        public static async Task<string> Get(string url, Dictionary<string, string>? headers = null) {
            var requestMessage = new HttpRequestMessage(HttpMethod.Get, url);

            // Add custom headers if provided
            if (headers != null) {
                foreach (var header in headers) {
                    if (!string.IsNullOrEmpty(header.Value)) {
                        requestMessage.Headers.Add(header.Key, header.Value);
                    }
                }
            }

            // Ensure the User-Agent and Authorization headers are included (if not already)
            requestMessage.Headers.Add("User-Agent", "MyCSharpApp");  // Default User-Agent

            try {
                // Send the request
                HttpResponseMessage response = await client.SendAsync(requestMessage);
                response.EnsureSuccessStatusCode(); // Throws an exception if not successful

                // Read and return the response body
                string responseBody = await response.Content.ReadAsStringAsync();
                return responseBody;
            }
            catch (HttpRequestException ex) {
                // Handle HTTP-specific errors
                Console.WriteLine($"Error: {ex.Message}");
                return string.Empty;  // Or return null, depending on your error handling strategy
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

        public static async Task GetFile(string url, string filepath, Dictionary<string, string> headers) {
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
    }
}
