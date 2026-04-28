using Avalonia.Controls;
using Avalonia.VisualTree;
using ModpackInstaller.Services;
using ModpackInstaller.ViewModels;

namespace ModpackInstaller.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        AttachedToVisualTree += (_, _) => {
            if (DataContext is MainViewModel vm) {
                if (vm.DialogService is DialogService ds) {
                    ds.AttachWindow(this);
                }
            }
        };
    }
}
