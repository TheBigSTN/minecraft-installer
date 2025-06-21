using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.ReactiveUI;
using ModpackInstaller.Services;
namespace ModpackInstaller.Desktop;

class Program
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static async Task<int> Main(string[] args) {

        if (args.Length > 0 && args[0] == "--update") {
            Console.WriteLine("Running in update mode...");

            await RunAutoUpdateLogic();

            LaunchTLauncher();

            return 0;
        }

        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);

        return 0;
    }

    static async Task RunAutoUpdateLogic() {
        List<ModpackService.Modpack> installedModpacks = ModpackService.ListInstalledModpacks();

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
