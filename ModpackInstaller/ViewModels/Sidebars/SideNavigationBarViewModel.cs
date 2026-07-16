using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Reactive;
using System.Text;
using System.Threading.Tasks;
using DynamicData;
using ModpackInstaller.Models;
using ModpackInstaller.Services.Modpack;
using ModpackInstaller.ViewModels.Dialogs;
using ReactiveUI;
using ReactiveUI.SourceGenerators;

namespace ModpackInstaller.ViewModels.Sidebars;
public partial class SideNavigationBarViewModel : ViewModelBase {
    private readonly MainViewModel _global;
    public ReactiveCommand<Unit, Unit> HomeCommand { get; }
    public ReactiveCommand<Unit, Unit> DiscoveryCommand { get; }
    public ReactiveCommand<ModpackMetadata, Unit> OpenModpackCommand { get; }
    public ReactiveCommand<Unit, Unit> CreateModpackCommand { get; }
    public ReactiveCommand<Unit, Unit> SettingsCommand { get; }

    public ObservableCollection<ModpackMetadata> Modpacks { get; }

    public enum NavigationPage
    {
        Home,
        Discovery,
        Settings
    }

    [Reactive] private NavigationPage _selectedPage;
    
    public SideNavigationBarViewModel(MainViewModel global) {
        var medatataService = new ModpackMedatataService();
        Modpacks = new ObservableCollection<ModpackMetadata>(ModpackMedatataService.LoadAll());
        _global = global;
        _selectedPage = NavigationPage.Home;

        HomeCommand = ReactiveCommand.Create( global.OpenHome );

        DiscoveryCommand = ReactiveCommand.Create(global.OpenDiscovery);
        
        OpenModpackCommand = ReactiveCommand.Create<ModpackMetadata>(global.OpenModpack);
        
        CreateModpackCommand = ReactiveCommand.CreateFromTask(async () => {
            var result = await _global.ShowDialog(new CreateModpackDialogViewModel(global));

            switch (result.Status) {
                case CreateModpackDialogResult.InstallModpack:
                    _global.OpenDiscovery();
                    break;
                case CreateModpackDialogResult.CustomSetup:
                    if (result.Response == null)
                        return;

                    ManualSetupPropData data = new() {
                        ModpackName = result.Response.Name,
                        GameVersion = result.Response.GameVersion,
                        Loader = result.Response.Loader,
                        LoaderVersion = result.Response.LoaderVersion
                    };
                    
                    if (await _global.ShowDialog(new ManualSetupDialogViewModel(data)) is not ModpackManualSetupResponse.Finished) 
                        return;
                    
                    ModpackMedatataService modpackMedatataService = new();

                    modpackMedatataService.Create(result.Response);
                    break;
                case CreateModpackDialogResult.ImportInstance:
                case CreateModpackDialogResult.Cancel:
                default: break;
            }
        });

        ModpackMedatataService.MetadataChanged += () => {
            Modpacks.Clear();
            Modpacks.AddRange(ModpackMedatataService.LoadAll());
        };
        
        SettingsCommand = ReactiveCommand.Create(() => {
            Console.WriteLine("Button pressed");
            Console.WriteLine(SelectedPage);
            //return Unit.Default;
            // global.ShowDiscovery();
        });
    }
}
