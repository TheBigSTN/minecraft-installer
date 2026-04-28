using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using ModpackInstaller.ViewModels.Dialogs;

namespace ModpackInstaller.Views;

public partial class Step2ConfigView : UserControl
{
    public Step2ConfigView()
    {
        InitializeComponent();
    }
    //private async void OnLoaderChanged( object? sender, SelectionChangedEventArgs e ) {
    //    if(DataContext is ConfigureStepViewModel vm) {
    //        await vm.SetLoaderAsync(vm.SelectedLoader);
    //    }
    //}

    //private async void OnLoaderVersionChanged( object? sender, SelectionChangedEventArgs e ) {
    //    if(DataContext is ConfigureStepViewModel vm) {
    //        await vm.SetLoaderVersionAsync(vm.SelectedLoaderVersion);
    //    }
    //}
}