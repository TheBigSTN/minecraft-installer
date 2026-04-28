using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using ModpackInstaller.Models;

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
#if DEBUG
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            "ModpackInstallerDev"
        );
#else
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "ModpackInstaller"
        );
#endif

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

    public static string GetBaseInstallPathFromLauncer( InstallPlatform installPlatform ) {
        string basePath = installPlatform switch {
            InstallPlatform.TLauncher => Environment.OSVersion.Platform switch {
                PlatformID.Win32NT => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), ".minecraft", "versions"),
                PlatformID.Unix => Path.Combine(Environment.GetEnvironmentVariable("HOME")!, ".minecraft", "versions"),
                PlatformID.MacOSX => Path.Combine(Environment.GetEnvironmentVariable("HOME")!, ".minecraft", "versions"),
                _ => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)!, ".minecraft", "versions")
            },
            InstallPlatform.CurseForge => Environment.OSVersion.Platform switch {
                PlatformID.Win32NT => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "curseforge", "minecraft", "Instances"),
                PlatformID.Unix => Path.Combine(Environment.GetEnvironmentVariable("HOME")!, ".curseforge", "minecraft", "Instances"),
                PlatformID.MacOSX => Path.Combine(Environment.GetEnvironmentVariable("HOME")!, "Library", "Application Support", "minecraft", "Instances"),
                _ => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)!, "curseforge", "minecraft", "Instances")
            },
            InstallPlatform.Modrinth => Environment.OSVersion.Platform switch {
                PlatformID.Win32NT => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ModrinthApp", "profiles"),
                PlatformID.Unix => Path.Combine(Environment.GetEnvironmentVariable("HOME")!, ".modrinth"),
                PlatformID.MacOSX => Path.Combine(Environment.GetEnvironmentVariable("HOME")!, "Library", "Application Support", "Modrinth"),
                _ => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)!, "ModrinthApp")
            },
            _ => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)!, "Minecraft")
        };
#if DEBUG
        return Path.Combine(basePath, "Test");
#else
            return basePath;
#endif
    }


}