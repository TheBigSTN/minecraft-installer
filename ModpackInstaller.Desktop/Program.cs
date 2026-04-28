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

class Program {
    [STAThread]
    public static int Main(string[] args) {
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

        try {
            BuildAvaloniaApp()
                .StartWithClassicDesktopLifetime(args);

            return 0;
        } catch (Exception ex) {
            CrashReporter.Log(ex, "Main");

            return -1;
        }
    }

    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace()
            .UseReactiveUI();
}