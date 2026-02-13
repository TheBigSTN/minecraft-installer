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
using ModpackInstaller.Infrastructure;
using Velopack;
namespace ModpackInstaller.Desktop;

class Program
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static int Main(string[] args) {

        // Velopack is important if it's not this important it either doesn't work or the process gets killed after 30s or so
        VelopackApp.Build().Run();

        try {
            BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
        }
        catch (Exception ex) {
            try {
                // === Setup folder ===
                string appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                string logFolder = Path.Combine(AppVariables.InstallerRoot, "crash_reports");
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

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace()
            .UseReactiveUI();
}
