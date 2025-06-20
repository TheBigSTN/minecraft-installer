using System;
using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Styling;
using ModpackInstaller.Services;

namespace ModpackInstaller.Views;

public partial class MainView : UserControl
{
    public MainView()
    {
        InitializeComponent();
        var modpacks = ModpackService.ListInstalledModpacks();
        if (modpacks.Count > 0) {
            AddModpackButtons(modpacks);
        }
        else {
            Sidebar.IsVisible = false;
            TopBar.IsVisible = false;
            Install_Button_Click(null, null);
        }
    }

    public void Install_Button_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs? e) {
        InstallMenu installModpackWindow = new();

        // Te abonezi la evenimentul onModpackInstalled
        installModpackWindow.OnModpackInstalled += InstallModpackWindow_ModpackInstalled;

        // Înlocuiește conținutul din BodyContent cu InstallMenu
        BodyContent.Content = installModpackWindow;
    }

    private void AddModpackButtons(List<ModpackService.Modpack> modpacks) {
        SidebarButtonPanel.Children.Clear();


        foreach (var modpack in modpacks) {

            var modpackButton = new Button {
                Content = modpack.MineLoader.ModpackName
            };

            modpackButton.Classes.Add("modpack-button");

            modpackButton.Click += (sender, e) =>
            {
                var detailsControl = new ModpackInfo(modpack);
                BodyContent.Content = detailsControl;
            };

            SidebarButtonPanel.Children.Add(modpackButton);
        }
    }

    private void InstallModpackWindow_ModpackInstalled(object? sender, EventArgs e) {
        // După ce instalarea modpack-ului este completă, poți reîncărca lista de modpack-uri
        var modpacks = ModpackService.ListInstalledModpacks();

        // Actualizează butoanele din sidebar pentru a reflecta modpack-urile noi
        AddModpackButtons(modpacks);

        BodyContent.Content = null;
        Sidebar.IsVisible = true;
        TopBar.IsVisible = true;
    }
}
