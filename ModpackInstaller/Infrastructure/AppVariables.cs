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

    public static string AppApiBaseUrl {
        get {
#if DEBUG
        return "http://192.168.0.189:8080";
#else
        return "https://minte.go.ro:5005/modpack-service";
#endif
        }
    }
}