using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace ModpackInstaller.Infrastructure;

public static class AppVariables {
    public static string GetTempFolderPath(string foldername) {
        string path = Path.Combine(Path.GetTempPath(), "ModpackInstaller", foldername);
        Directory.CreateDirectory(path);
        return path;
    }
    public static string GetTempFilePath(string filename) {
        string path = Path.Combine(Path.GetTempPath(), "ModpackInstaller", filename);
        Directory.CreateDirectory(Path.Combine(path, ".."));
        return path;
    }
    public static string InstallerRoot { get; } =
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "ModpackInstaller"
        );
    public static JsonSerializerOptions DefaultJsonOptions => new() {
        WriteIndented = true
    };

    public static JsonSerializerOptions WebJsonOptions => new() {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        Converters = {
            new JsonStringEnumConverter()
        }
    };
//#if DEBUG
//    public static string AppApiBaseUrl { get; } = "http://localhost:8080";
//#else
    public static string AppApiBaseUrl { get; } = "https://minte.go.ro:5005";
//#endif
}