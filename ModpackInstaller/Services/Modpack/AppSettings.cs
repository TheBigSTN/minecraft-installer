using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using ModpackInstaller.Infrastructure;
using ModpackInstaller.Models;

namespace ModpackInstaller.Services.Modpack;

public class AppSettings {
    public static readonly AppSettings Settings = new();
    
    private readonly string _configPath;

    // Configul real
    public AppConfig Config { get; private set; }
    public AppSettings() {
        _configPath = Path.Combine(AppVariables.InstallerRoot, "appsettings.json");

        if (File.Exists(_configPath)) {
            try {
                var json = File.ReadAllText(_configPath);
                var cfg = JsonSerializer.Deserialize<AppConfig>(json);
                Config = cfg ?? new AppConfig();
            }
            catch {
                Config = new AppConfig();
                Save(); // scriem fișierul default dacă a eșuat citirea
            }
        }
        else {
            Directory.CreateDirectory(_configPath);
            Config = new AppConfig();
            Save(); // scriem fișier default dacă nu exista
        }
    }
    
    private void Save() {
        var json = JsonSerializer.Serialize(Config, AppVariables.DefaultJsonOptions);
        File.WriteAllText(_configPath, json);
    }

    public void Update(Action<AppConfig> update) {
        update(Config);
        Save();
    }

    public void SetInstallTarget(InstallPlatform platform) {
        Update(config => config.InstallTarget = platform);
    }

    public void Reset() {
        Config = new AppConfig();
        Save();
    }
}

public class AppConfig {
    public InstallPlatform InstallTarget { get; set; } = InstallPlatform.CurseForge;
    public string? UserPasswordToken { get; set; }
    
    public Guid? UserId { get; set; }
    
    public string? UserName { get; set; }
}