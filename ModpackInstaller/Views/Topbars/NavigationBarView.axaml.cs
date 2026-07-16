using System.Reflection;
using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace ModpackInstaller.Views;

public partial class NavigationBarView : UserControl {
    
    public NavigationBarView() {
        InitializeComponent();

        var raw = Assembly
            .GetEntryAssembly()?
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
            .InformationalVersion;

        Version.Text = $"v{raw?.Split('+')[0]}";
    }

    private Window? Window => TopLevel.GetTopLevel(this) as Window;

    private void Close( object? sender, RoutedEventArgs e ) {
        Window?.Close();
    }

    private void Minimize( object? sender, RoutedEventArgs e ) {
        if(Window != null)
            Window.WindowState = WindowState.Minimized;
    }

    private void Maximize( object? sender, RoutedEventArgs e ) {
        if(Window == null)
            return;

        Window.WindowState =
            Window.WindowState == WindowState.Maximized
                ? WindowState.Normal
                : WindowState.Maximized;
    }

    private void Drag( object? sender, PointerPressedEventArgs e ) {
        Window?.BeginMoveDrag(e);
    }
}