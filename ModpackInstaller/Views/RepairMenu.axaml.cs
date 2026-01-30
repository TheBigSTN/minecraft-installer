using System;
using System.IO;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using ModpackInstaller.Services;
using MsBox.Avalonia.Dto;
using MsBox.Avalonia.Enums;
using MsBox.Avalonia;
using System.Linq;
using System.Text.Json;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace ModpackInstaller;

public partial class RepairMenu : UserControl
{
    public event Action? OnFinished;
    public RepairMenu()
    {
        InitializeComponent();
        Loaded += PopulateDropDown;
        Modpack_List.SelectionChanged += ModpackList_SelectionChanged;
    }

    private async void PopulateDropDown(object? sender, RoutedEventArgs e)
    {
        string[] modpacks = Directory.GetDirectories(ModpackService.modpacksPath).Select(path => Path.GetFileName(path)).ToArray(); ;

        var remoteModpacks = await Github.GetAllRemoteModpacks();

        var repairableModpacks = remoteModpacks.Tree
            .Where(i => modpacks.Any(m => m == i.Path))
            .ToArray();

        foreach (var t in repairableModpacks)
        {
            Modpack_List.Items.Add(t.Path);
        }
        Modpack_List.IsVisible = true;
        Loading_Text.IsVisible = false;
    }

    private void ModpackList_SelectionChanged(object? sender, SelectionChangedEventArgs e) {
        Repair_Button.IsEnabled = true;
    }

    private async void Repair_Click(object sender, RoutedEventArgs e) {
        Repair_Button.IsEnabled = false;

        string? modpack_name = Modpack_List.SelectedItem?.ToString();

        if (modpack_name == null)
            return;

        Modpack modpack = new(modpack_name);

        ModpackService.TLauncherData tlauncherData = modpack.GetLauncherData();

        ModpackService.MineLoaderData mineLoaderData = new() {
            ModpackName = Path.GetFileName(modpack.installLocation),
            FileTree = await modpack.GetModpackRemoteFileTree(),
            Mods = tlauncherData.Mods

        };

        using (FileStream mineloaderWriteStream = File.Create(modpack.mineloaderAdditionalPath)) {
            await JsonSerializer.SerializeAsync(mineloaderWriteStream, mineLoaderData, ModpackService.jsonOptions);
        }

        await ModpackUpdater.Update(modpack.modpackId);
        
        var msg = MessageBoxManager
        .GetMessageBoxStandard(new MessageBoxStandardParams {
            ButtonDefinitions = ButtonEnum.Ok,
            Icon = Icon.Success,
            ContentTitle = "Succes!",
            ContentMessage = "Modpackul as fost reparat cu succes!",
            WindowStartupLocation = WindowStartupLocation.CenterOwner
        });

        await msg.ShowAsync();
        OnFinished?.Invoke();
        Repair_Button.IsEnabled = true;
    }
} 