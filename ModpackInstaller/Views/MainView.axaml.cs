using System;
using System.Collections.Generic;
using System.IO;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Styling;
using Avalonia.VisualTree;
using ModpackInstaller.Infrastructure;
using ModpackInstaller.Models;
using ModpackInstaller.Services;
using ModpackInstaller.Services.Modpack;
using ModpackInstaller.ViewModels;
using ModpackInstaller.ViewModels.Body;
using ModpackInstaller.ViewModels.Topbars;
using ModpackInstaller.Windows;
using MsBox.Avalonia.Enums;
using MsBox.Avalonia;
using MsBox.Avalonia.Dto;
using System.Diagnostics;

namespace ModpackInstaller.Views;

public partial class MainView : UserControl
{
    public MainView()
    {
        InitializeComponent();

        //this.AttachedToVisualTree += (_, _) => {
        //    if (DataContext is not MainViewModel mainVm)
        //        return;

        //    if (mainVm.TopBarView is GlobalTopBarView topBar &&
        //        topBar.DataContext is GlobalTopBarViewModel topBarVm) {
        //        topBarVm.ShowCreateModpackDialog += async () => {
        //            var window = this.GetVisualRoot() as Window;
        //            var dialog = new CreateModpackWindow() {
        //                DataContext = new CreateModpackViewModel()
        //            };
        //            return await dialog.ShowDialog<ModpackMetadata?>(window);
        //        };
        //    }
        //};
        this.AttachedToVisualTree += (_, _) => {
            if (DataContext is not MainViewModel mainVm)
                return;

            if (mainVm.TopBarViewModel is GlobalTopBarViewModel topBarVm) {
                topBarVm.ShowCreateModpackDialog += async () => {
                    var window = this.GetVisualRoot() as Window;

                    // creez ViewModel
                    var vm = new CreateModpackViewModel(mainVm.InstallPath);

                    // creez dialog
                    var dialog = new CreateModpackWindow {
                        DataContext = vm
                    };

                    // wiring: când ViewModel vrea să se închidă
                    vm.CloseRequested += result => {
                        dialog.Close(result);
                    };

                    // afișez dialog și aștept rezultatul
                    var metadata = await dialog.ShowDialog<ModpackMetadata?>(window);

                    // 🔹 dacă user a creat efectiv un modpack, salvez
                    if (metadata != null) {
                        var messageBoxCustomParams = new MessageBoxStandardParams {
                            ContentTitle = "Confirmare Creare Modpack",
                            ContentMessage =    $"Because of development reasons before creating the modpack '{metadata.Name}'?\n\n" +
                                                $"You need in the app {mainVm.InstallTarget} to create a modpack with:\n" +
                                                $"The name: {metadata.Name}\n" +
                                                $"The Minecraft version: {metadata.GameVersion}\n" +
                                                $"The loader: {metadata.Loader}\n" +
                                                $"The loader version: {metadata.LoaderVersion}",
                            ButtonDefinitions = ButtonEnum.YesNo,
                            Icon = Icon.Info,
                            WindowStartupLocation = WindowStartupLocation.CenterOwner
                        };

                        var messageBox = MessageBoxManager.GetMessageBoxStandard(messageBoxCustomParams);
                        var result = await messageBox.ShowWindowAsync();

                        // 2. Dacă utilizatorul a ales "No", oprim execuția
                        if (result == ButtonResult.No ||
                            result == ButtonResult.Abort) {
                            return null;
                        }
                        Debug.WriteLine(result);


                        // instanță ModpackMedatataService cu path-ul principal
                        var registry = new ModpackMedatataService(AppVariables.InstallerRoot);

                        // creez / salvez metadata
                        registry.Create(metadata);

                        // 🔹 eventual: deschide modpack-ul în UI
                        mainVm.OpenModpack(metadata);

                        // 🔹 eventual: refresh listă modpack-uri în UI
                        mainVm.RefreshModpackList();
                    }

                    return metadata;
                };
            }
        };
    }
}
