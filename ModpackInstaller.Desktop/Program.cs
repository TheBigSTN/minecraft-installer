using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.ReactiveUI;
using ModpackInstaller.Infrastructure;
using Velopack;
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
                    }
                })
				.Run();
        } catch (Exception ex) {
			CrashReporter.Log(ex, "Velopack");
			return -1;
		}

		InitializeLogging();

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
	        Console.WriteLine("========== STARTED THE UI ==========");
			BuildAvaloniaApp()
				.StartWithClassicDesktopLifetime(args);

			return 0;
		} catch (Exception ex) {
			CrashReporter.Log(ex, "Main");

			return -1;
		}
	}
    
	private static void InitializeLogging()
	{
		var logDirPath = Path.Combine(AppVariables.InstallerRoot, "logs");
		Directory.CreateDirectory(logDirPath);

		var date = DateTime.Now.ToString("yyyy-MM-dd");

		var latestLogPath = Path.Combine(logDirPath, "latest.log");
		var dailyLogPath = Path.Combine(logDirPath, $"{date}.log");
		var latestDebugLogPath = Path.Combine(logDirPath, "debug.log");
		var dailyDebugLogPath = Path.Combine(logDirPath, $"debug-{date}.log");

		var consoleWriter = TextWriter.Synchronized(new MultiTextWriter(
			new StreamWriter(latestLogPath, false) { AutoFlush = true },
			new StreamWriter(dailyLogPath, true) { AutoFlush = true }));

		var debugWriter = TextWriter.Synchronized(new MultiTextWriter(
			new StreamWriter(latestDebugLogPath, false) { AutoFlush = true },
			new StreamWriter(dailyDebugLogPath, true) { AutoFlush = true }));

		Console.SetOut(consoleWriter);
		Console.SetError(consoleWriter);

		Trace.Listeners.Clear();
		Trace.Listeners.Add(new TextWriterTraceListener(debugWriter));
		Trace.AutoFlush = true;
	}

    private static void SetupWindowsPath() {
        var cliDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "ModpackInstaller",
                "cli"
            );

        var cliCmdPath = Path.Combine(cliDir, "mpk.cmd");

        if (!File.Exists(cliCmdPath)) 
            CreateWindowsCliWrapper();

        var currentPath = Environment.GetEnvironmentVariable("PATH", EnvironmentVariableTarget.User) ?? "";

        if(currentPath.Contains(cliDir))
            return;

        var newPath = currentPath + ";" + cliDir;

        Environment.SetEnvironmentVariable(
            "PATH",
            newPath,
            EnvironmentVariableTarget.User
        );

        Console.WriteLine("CLI folder added to PATH (restart terminal required).");
    }

    private static void CreateWindowsCliWrapper() {
        var installDir = AppContext.BaseDirectory;

        // pick a stable location for CLI shim
        var cliDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "ModpackInstaller",
            "cli"
        );

        Directory.CreateDirectory(cliDir);

        var cmdPath = Path.Combine(cliDir, "mpk.cmd");

        var exePath = Path.Combine(installDir, "ModpackInstaller.Desktop.exe");

        var content =
	        $"""
	         @echo off
	         		"{exePath}" %*
	         		
	         """;

        File.WriteAllText(cmdPath, content);

        Console.WriteLine($"CLI wrapper created at: {cmdPath}");
    }

    [DllImport("kernel32.dll")] private static extern bool AttachConsole( int dwProcessId );

    public static AppBuilder BuildAvaloniaApp()
		=> AppBuilder.Configure<App>()
			.UsePlatformDetect()
			.WithInterFont()
			.LogToTrace()
			.UseReactiveUI();
}