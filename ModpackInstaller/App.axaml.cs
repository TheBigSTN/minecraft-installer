using System;
using System.Diagnostics;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using Microsoft.Extensions.DependencyInjection;
using ModpackInstaller.Infrastructure;
using ModpackInstaller.Services;
using ModpackInstaller.ViewModels;
using ModpackInstaller.Views;

namespace ModpackInstaller;

public partial class App : Application {
    public IServiceProvider Services { get; private set; } = null!;
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted() {
        Dispatcher.UIThread.UnhandledException += (s, e) =>
        {
            CrashReporter.Log(e.Exception, "UIThread");

#if DEBUG
            //if (Debugger.IsAttached)
            //    Debugger.Break();
#endif

            e.Handled = true;
        };
        //var services = new ServiceCollection();

        //services.AddSingleton<IDialogService, DialogService>();
        //services.AddSingleton<MainViewModel>();

        //Services = services.BuildServiceProvider();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var dialogService = new DialogService();
            var viewModel = new MainViewModel(dialogService);

            var window = new MainWindow {
                DataContext = viewModel
            };

            dialogService.AttachWindow(window);

            desktop.MainWindow = window;
        }
        else if (ApplicationLifetime is ISingleViewApplicationLifetime singleViewPlatform)
        {
            singleViewPlatform.MainView = new MainView
            {
                DataContext = new MainViewModel(new DialogService())
            };
        }

        base.OnFrameworkInitializationCompleted();
    }
}
