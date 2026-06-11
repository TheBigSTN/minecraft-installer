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
using Velopack.Sources;
namespace ModpackInstaller.Desktop;

class Program {
	[STAThread]
    public static async Task<int> Main( string[] args ) {
        // Global crash hooks FIRST
        AppDomain.CurrentDomain.UnhandledException += (s, e) => {
			CrashReporter.Log(e.ExceptionObject as Exception, "AppDomain");
		};

		TaskScheduler.UnobservedTaskException += (s, e) => {
			CrashReporter.Log(e.Exception, "TaskScheduler");
			e.SetObserved();
		};

		try {
			VelopackApp.Build()
				.OnFirstRun((v) => {
                    if(OperatingSystem.IsWindows()) {
                        SetupWindowsPath();
                    } else if(OperatingSystem.IsLinux()) {
                        SetupLinuxSymlink();
                    }
                })
				.Run();
        } catch (Exception ex) {
			CrashReporter.Log(ex, "Velopack");
			return -1;
		}

        // Dacă avem argumente SAU am reușit să ne atașăm, rulăm CLI
        if(args.Length > 0) {

            if(RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
                AttachConsole(-1);
            }
            Console.WriteLine();
            await CliRunner.RunAsync(args);
            Console.WriteLine();
            return 0;
        }

        try {
			BuildAvaloniaApp()
				.StartWithClassicDesktopLifetime(args);

			return 0;
		} catch (Exception ex) {
			CrashReporter.Log(ex, "Main");

			return -1;
		}
	}

    private static void SetupWindowsPath() {
        var installDir = AppContext.BaseDirectory;
       
        var path = Environment.GetEnvironmentVariable("PATH", EnvironmentVariableTarget.User) ?? "";

        if(path.Contains(installDir))
            return;

        var newPath = path + ";" + installDir;

        Environment.SetEnvironmentVariable(
            "PATH",
            newPath,
            EnvironmentVariableTarget.User
        );

        Console.WriteLine("Added to PATH (user level). Restart terminal required.");
    }

    private static void SetupLinuxSymlink() {
        var installDir = AppContext.BaseDirectory;
        var exePath = Path.Combine(installDir, "modpack-installer");

        var target = "/usr/local/bin/modpack";

        try {
            if(File.Exists(target))
                return;

            var psi = new ProcessStartInfo {
                FileName = "ln",
                Arguments = $"-s {exePath} {target}",
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            Process.Start(psi)?.WaitForExit();

            Console.WriteLine("Created symlink in /usr/local/bin");
        } catch(Exception ex) {
            Console.WriteLine($"Failed to create symlink: {ex.Message}");
        }
    }

    [DllImport("kernel32.dll")] private static extern bool AttachConsole( int dwProcessId );

    public static AppBuilder BuildAvaloniaApp()
		=> AppBuilder.Configure<App>()
			.UsePlatformDetect()
			.WithInterFont()
			.LogToTrace()
			.UseReactiveUI();
}