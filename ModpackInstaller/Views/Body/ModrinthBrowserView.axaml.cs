using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using ModpackInstaller.ViewModels.Body;

namespace ModpackInstaller.Views.Body;

public partial class ModrinthBrowserView : UserControl
{
    public ModrinthBrowserView()
    {
        InitializeComponent();
    }
    public void ListBox_ScrollChanged(object? sender, ScrollChangedEventArgs e) {
        // 'sender' este ListBox-ul. Trebuie să ajungem la ScrollViewer-ul lui.
        // În Avalonia, ListBox-ul implementează adesea logica prin ScrollViewer-ul intern.
        if (sender is ListBox) {
            // Cea mai sigură metodă: extragem valorile din eveniment dacă sunt disponibile 
            // sau căutăm ScrollViewer-ul în structura vizuală.

            if (e.Source is ScrollViewer scroll) {
                double currentPos = scroll.Offset.Y;
                double visibleArea = scroll.Viewport.Height;
                double totalHeight = scroll.Extent.Height;

                // Verificăm dacă suntem aproape de fund (totalHeight > 0 previne declanșarea pe liste goale)
                if (totalHeight > 0 && currentPos + visibleArea >= totalHeight - 150) {
                    if (DataContext is ModrinthBrowserViewModel vm) {
                        _ = vm.SearchAsync(append: true);
                    }
                }
            }
        }
    }
}