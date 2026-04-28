using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;

namespace ModpackInstaller.Views;

public partial class ExportPickerView : Window
{
    public ExportPickerView()
    {
        InitializeComponent();
    }

    private void OnCancelClick( object? sender, RoutedEventArgs e ) {
        Close(null);
    }

}