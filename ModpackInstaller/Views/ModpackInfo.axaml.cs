using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Styling;
using ModpackInstaller.Services;
using MsBox.Avalonia;
using MsBox.Avalonia.Dto;
using MsBox.Avalonia.Enums;

namespace ModpackInstaller.Views;

public partial class ModpackInfo : UserControl {
    private string modpackName;
    public ModpackInfo(ModpackService.Modpack modpack) {
        InitializeComponent();
        ModpackNameText.Text = modpack.MineLoader.ModpackName;
        modpackName = modpack.MineLoader.ModpackName;
    }

    public ModpackInfo() {
        InitializeComponent();
        ModpackNameText.Text = "Unknown Modpack";
        modpackName = "Unknown";
    }

    private async void CheckUpdate_Click(object sender, RoutedEventArgs e) {
        CheckUpdateButton.IsEnabled = false;
        Modpack modpack = new(modpackName);

        if (await modpack.IsOutdated()) {

            var mb = MessageBoxManager
                .GetMessageBoxStandard(new MessageBoxStandardParams {
                    ButtonDefinitions = ButtonEnum.YesNo,
                    Icon = Icon.Question,
                    ContentTitle = "Update Available",
                    ContentMessage = "An update is available for this modpack.\nDo you want to update now?",
                    WindowStartupLocation = WindowStartupLocation.CenterOwner
                });

            var result = await mb.ShowAsync();

            if (result == ButtonResult.Yes) {
                await modpack.UpdateModpack();
            }
        } else {
            var mb = MessageBoxManager
                .GetMessageBoxStandard(new MessageBoxStandardParams {
                    ButtonDefinitions = ButtonEnum.Ok,
                    Icon = Icon.Info,
                    ContentTitle = "Up to date",
                    ContentMessage = "No update is available.",
                    WindowStartupLocation = WindowStartupLocation.CenterOwner
                });

            await mb.ShowAsync();
        }
        CheckUpdateButton.IsEnabled = true;
    }

}
