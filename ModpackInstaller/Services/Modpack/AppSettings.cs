using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using ModpackInstaller.Infrastructure;
using ModpackInstaller.Models;

namespace ModpackInstaller.Services.Modpack;

public class AppSettings {
    private readonly string _configPath;

    // Configul real
    public AppConfig Config { get; private set; }

    // Constructor: încarcă sau creează default automat
    public AppSettings(string appRoot) {
        _configPath = Path.Combine(appRoot, "appsettings.json");

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
            Directory.CreateDirectory(appRoot);
            Config = new AppConfig();
            Save(); // scriem fișier default dacă nu exista
        }
    }

    // Salvează config-ul pe disk
    private void Save() {
        var json = JsonSerializer.Serialize(Config, AppVariables.DefaultJsonOptions);
        File.WriteAllText(_configPath, json);
    }

    // Update parțial și sigur
    public void Update(Action<AppConfig> update) {
        update(Config);
        Save(); // salvează imediat după modificare
    }

    // Reset la default
    public void Reset() {
        Config = new AppConfig();
        Save();
    }
}

// Clasa de config efectivă
public class AppConfig {
    public InstallPlatform InstallTarget { get; set; } = InstallPlatform.CurseForge;

    // Aceasta este "Parola User-ului" (Owner Token) primită la register
    public string? UserPasswordToken { get; set; }

    // Putem stoca și nickname-ul pentru a-l afișa în UI fără a interoga serverul
    public string? UserNickname { get; set; }
}