using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls;
using Avalonia.ReactiveUI;
using Avalonia.Threading;
using ModpackInstaller.Services;
using System.Reflection;
namespace ModpackInstaller.Desktop;

class Program
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static async Task<int> Main(string[] args) {

        if (args.Length > 0 && args[0] == "--update") {

            await RunAutoUpdateLogic();

            LaunchTLauncher();
            return 0;
        }

        try {
            BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
        }
        catch (Exception ex) {
            try {
                // === Setup folder ===
                string appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                string logFolder = Path.Combine(appData, "ModpackInfo Installer", "logs");
                Directory.CreateDirectory(logFolder);

                // === Generate unique filename ===
                string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
                string logFileName = $"crash-{timestamp}.log";
                string logPath = Path.Combine(logFolder, logFileName);

                // === Get extra info ===
                string appVersion = Assembly.GetExecutingAssembly()
                                            .GetName()
                                            .Version?.ToString() ?? "unknown";
                string dotnetVersion = Environment.Version.ToString();
                string os = Environment.OSVersion.ToString();
                string arch = RuntimeInformation.OSArchitecture.ToString();
                string processArch = RuntimeInformation.ProcessArchitecture.ToString();

                // === Build crash report ===
                string report = $"""
                    ========== ModpackInfo Installer Crash Report ==========
                    Timestamp     : {DateTime.Now}
                    App Version   : {appVersion}
                    .NET Version  : {dotnetVersion}
                    OS            : {os} ({arch})
                    Process Arch  : {processArch}
                    ====================================================

                    Exception:
                    {ex}

                    ====================================================
                """;

                // === Save log ===
                File.WriteAllText(logPath, report);
            }
            catch { }
        }

        return 0;
    }

    static async Task RunAutoUpdateLogic() {
        List<ModpackService.ModpackInfo> installedModpacks = ModpackService.ListInstalledModpacks();

        var updateTasks = installedModpacks
            .Where(mp => mp.MineLoader.AutoUpdate)
            .Select(mp => ModpackUpdater.Update(mp.MineLoader.ModpackName));
        await Task.WhenAll(updateTasks);
    }

    static void LaunchTLauncher() {
        string tlauncherPath = GetTLauncherPath();

        if (!File.Exists(tlauncherPath)) {
            Console.WriteLine($"TLauncher nu a fost găsit la: {tlauncherPath}");
            return;
        }

        try {
            Process.Start(new ProcessStartInfo {
                FileName = tlauncherPath,
                UseShellExecute = true // pentru macOS și Windows
            });

            Console.WriteLine("TLauncher pornit.");
        }
        catch (Exception ex) {
            Console.WriteLine($"Eroare la pornirea TLauncher: {ex.Message}");
        }
    }

    static string GetTLauncherPath() {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
            return Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                ".minecraft",
                "TLauncher.exe"
            );
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) {
            return "/Applications/TLauncher.app"; // sau caută în alt path
        }
        else // Linux
        {
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".tlauncher", "TLauncher.sh");
        }
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace()
            .UseReactiveUI();
}
