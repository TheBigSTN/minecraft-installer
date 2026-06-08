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
			VelopackApp.Build().Run();
        } catch (Exception ex) {
			CrashReporter.Log(ex, "Velopack");
			return -1;
		}

        // Dacă avem argumente SAU am reușit să ne atașăm, rulăm CLI
        if(args.Length > 0) {
            AttachConsole(-1);
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

    [DllImport("kernel32.dll")] private static extern bool AttachConsole( int dwProcessId );

    public static AppBuilder BuildAvaloniaApp()
		=> AppBuilder.Configure<App>()
			.UsePlatformDetect()
			.WithInterFont()
			.LogToTrace()
			.UseReactiveUI();
}