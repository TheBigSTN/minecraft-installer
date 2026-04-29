using System.Reflection;
using Avalonia.Controls;
using Avalonia.VisualTree;
using ModpackInstaller.Services;
using ModpackInstaller.ViewModels;
using Velopack;

namespace ModpackInstaller.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        var version = Assembly
            .GetExecutingAssembly()
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
            ?.InformationalVersion + " - " + VelopackRuntimeInfo.VelopackNugetVersion?.ToString();

        Title = $"Modpack Installer v{version}";

        AttachedToVisualTree += (_, _) => {
            if (DataContext is MainViewModel vm) {
                if (vm.DialogService is DialogService ds) {
                    ds.AttachWindow(this);
                }
            }
        };
    }
}
