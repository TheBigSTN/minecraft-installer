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

        var raw = Assembly
            .GetEntryAssembly()
            ?.GetCustomAttribute<AssemblyInformationalVersionAttribute>()
            ?.InformationalVersion;

        var version = raw?.Split('+')[0];

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
