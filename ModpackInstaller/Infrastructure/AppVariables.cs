using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using ModpackInstaller.Converters;
using ModpackInstaller.Models;
using ModpackInstaller.Models.Modrinth;
using ModpackInstaller.Services.Helpers;
using Environment = System.Environment;

namespace ModpackInstaller.Infrastructure;

public static class AppVariables {
    public static string GetTempFolderPath(string foldername) {
        var path = Path.Combine(Path.GetTempPath(), "ModpackInstaller", foldername);
        Directory.CreateDirectory(path);
        return path;
    }
    public static string GetTempFilePath(string filename) {
        var path = Path.Combine(Path.GetTempPath(), "ModpackInstaller", filename);
        Directory.CreateDirectory(Path.Combine(path, ".."));
        return path;
    }
#pragma warning disable CS0169 // Field is never used
    //It's used only on releases and on in a dev environment
    private static string? _installerRoot;
#pragma warning restore CS0169 // Field is never used
    public static string InstallerRoot {
        get {
#if DEBUG
        return Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                "ModpackInstallerDev");
#else
            if (_installerRoot != null) 
                return _installerRoot;
            
            var baseLocal = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

            var localDataPath = Path.Combine(baseLocal, "ModpackInstaller");
            
            _installerRoot = localDataPath;
            
            return _installerRoot;

#endif
        }
    }

    public static readonly JsonSerializerOptions DefaultJsonOptions = new() {
        WriteIndented = true,
        Converters = {
            new FlexibleGuidConverter(),
            new FlexibleNullableGuidConverter(),
            new MigratedDataJsonConverterFactory()
        }
    };

    public static readonly JsonSerializerOptions WebJsonOptions = new() {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        Converters = {
            // new JsonStringEnumConverter(), this is kinda debug removal since i don't know if i need it
            new MigratedDataJsonConverterFactory(),
            new UniversalEnumConverterFactory()
        }
    };


    public static string AppApiBaseUrl {
        get {
#if DEBUG
            // return "http://192.168.0.189:8080/";
            return "http://localhost:8080/";
#else
        return "https://minte.go.ro:5005/modpack-service/";
#endif
        }
    }

    public static string GetBaseInstallPathFromLauncer( InstallPlatform installPlatform ) {
        var basePath = installPlatform switch {
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