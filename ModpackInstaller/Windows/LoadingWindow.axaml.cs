using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace ModpackInstaller;

public partial class LoadingWindow : Window
{
    public LoadingWindow()
    {
        InitializeComponent();
    }

    public void ChangeText(string text) {
        StatusText.Text = text;
    }
}