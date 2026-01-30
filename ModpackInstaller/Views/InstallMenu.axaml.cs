using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using ModpackInstaller.Services;
using MsBox.Avalonia.Dto;
using MsBox.Avalonia.Enums;
using MsBox.Avalonia;

namespace ModpackInstaller;

public partial class InstallMenu : UserControl {
    public event EventHandler? OnModpackInstalled;
    public InstallMenu() {
        InitializeComponent();
        Loaded += PopulateDropDown;
        Modpack_List.SelectionChanged += ModpackList_SelectionChanged;
    }

    private async Task<List<GitHubTreeItem>> GetTreeItems() {
        var main = await Github.GetGitHubTreeAsync(false);
        var tree = main.Tree.Where(t => t.Type.Equals("tree")).ToList();
        return tree;
    }

    private async void PopulateDropDown(object? sender, RoutedEventArgs e) {
        var treeItems = await GetTreeItems();
        foreach (var t in treeItems) {
            Modpack_List.Items.Add(t.Path);
        }
        Modpack_List.IsVisible = true;
        Loading_Text.IsVisible = false;
    }

    private void ModpackList_SelectionChanged(object? sender, SelectionChangedEventArgs e) {
        Install_Button.IsEnabled = true;
    }

    private async void Install(object sender, RoutedEventArgs e) {
        Install_Button.IsEnabled = false;
        var treeItems = await GetTreeItems();

        string? modpack_name = Modpack_List.SelectedItem?.ToString();
        var modpackTree = treeItems.Find(i => i.Path.Equals(modpack_name));

        if (modpack_name == null || modpackTree == null)
            return;

        Modpack modpack = new(modpack_name);

        if (Directory.Exists(modpack.installLocation)) {
            var mb = MessageBoxManager
                    .GetMessageBoxStandard(new MessageBoxStandardParams {
                        ButtonDefinitions = ButtonEnum.YesNo,
                        Icon = Icon.Warning,
                        ContentTitle = "ModpackInfo deja instalat",
                        ContentMessage = $"ModpackInfo ul \"{modpack_name}\" este deja instalat. Vrei sa il rescrii?",
                        WindowStartupLocation = WindowStartupLocation.CenterOwner
                    });

            var result = await mb.ShowAsync();

            if (result == ButtonResult.No) {
                return;
            }
        }
        
        await Github.DownloadModpack(modpackTree.Sha, modpack_name, modpack.GetMineLoaderData().AutoUpdate);

        var msg = MessageBoxManager
        .GetMessageBoxStandard(new MessageBoxStandardParams {
            ButtonDefinitions = ButtonEnum.Ok,
            Icon = Icon.Success,
            ContentTitle = "Succes!",
            ContentMessage = "ModpackInfo Installed if you have tlauncer open press the reload button",
            WindowStartupLocation = WindowStartupLocation.CenterOwner
        });

        await msg.ShowAsync();
        OnModpackInstalled?.Invoke(this, EventArgs.Empty);
        Install_Button.IsEnabled = true;
    }
}