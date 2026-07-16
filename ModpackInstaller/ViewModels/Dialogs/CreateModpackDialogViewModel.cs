using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using DynamicData;
using ModpackInstaller.Infrastructure;
using ModpackInstaller.Models;
using ModpackInstaller.Services;
using ModpackInstaller.Services.Modpack;
using ReactiveUI;
using ReactiveUI.SourceGenerators;

namespace ModpackInstaller.ViewModels.Dialogs;

public partial class CreateModpackDialogViewModel
    : DialogViewModel<DialogResponse<CreateModpackDialogResult, ModpackMetadata?>> {
    private readonly MainViewModel _main;
    public ReactiveCommand<Unit, Unit> CustomSetupCommand { get; }
    
    public ReactiveCommand<Unit, Unit> CreateInstanceCommand { get; }
    public ReactiveCommand<Unit, Unit> InstallModpackCommand { get; }
    public ReactiveCommand<Unit, Unit> ImportInstanceCommand { get; }
    public ReactiveCommand<Unit, Unit> CloseCommand { get; }
    
    [Reactive]
    private ModLoaderType? _selectedLoader;
    
    [Reactive]
    private InstallPlatform _selectedModpackLoader;

    [Reactive] 
    private string? _modpackName;
    
    private List<GameVersionEntryDto>? _loaderManifestVersions;
    public ObservableCollection<string> GameVersions { get; } = [];

    [Reactive] private string? _selectedGameVersion;

    public ObservableCollection<string> LoaderVersions { get; } = [];
    
    [Reactive] private string? _selectedLoaderVersion;

    public CreateModpackDialogViewModel(MainViewModel main) {
        _main = main;
        SelectedModpackLoader = AppSettings.Settings.Config.InstallTarget;
        
        
        CustomSetupCommand = ReactiveCommand.CreateFromTask(async () => {
            SelectedLoader = ModLoaderType.NeoForge;
            await LoadGameVersionsAsync(SelectedLoader ?? ModLoaderType.NeoForge);
        });

        CreateInstanceCommand = ReactiveCommand.Create(CreateInstance);

        InstallModpackCommand = ReactiveCommand.Create(() =>
            Close(new DialogResponse<CreateModpackDialogResult, ModpackMetadata?> {
                Status =  CreateModpackDialogResult.InstallModpack
            }));

        ImportInstanceCommand = ReactiveCommand.Create(() =>
            Close(new DialogResponse<CreateModpackDialogResult, ModpackMetadata?> {
                Status =  CreateModpackDialogResult.ImportInstance
            }));

        CloseCommand = ReactiveCommand.Create(() =>
            Close(new DialogResponse<CreateModpackDialogResult, ModpackMetadata?> {
                Status =  CreateModpackDialogResult.Cancel
            }));
        
        this.WhenAnyValue(t => t.SelectedModpackLoader)
            .Skip(1)
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(loader => {
                AppSettings.Settings.SetInstallTarget(loader);
            });

        this.WhenAnyValue(t => t.SelectedGameVersion)
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(LoadLoaderVersions);
        
        this.WhenAnyValue(t => t.SelectedLoader)
            .ObserveOn(RxApp.MainThreadScheduler)
            .InvokeCommand(ReactiveCommand.CreateFromTask<ModLoaderType>(LoadGameVersionsAsync));
    }

    private void CreateInstance() {
        if (SelectedGameVersion == null ||
            SelectedLoader == null ||
            SelectedLoaderVersion == null ||
            ModpackName == null ||
            SelectedLoader == null)
            return;
            
        var loader = SelectedLoader.Value;
            
        var metadata = new ModpackMetadata {
            Id = Guid.NewGuid(),
            Name = ModpackName.Trim(),
            GameVersion = SelectedGameVersion,
            Loader = loader,
            LoaderVersion = SelectedLoaderVersion,
            InstallPath = Path.Combine(AppVariables.GetBaseInstallPathFromLauncer(SelectedModpackLoader), ModpackName.Trim())
        };

        

        Close(new DialogResponse<CreateModpackDialogResult, ModpackMetadata?> {
            Status = CreateModpackDialogResult.CustomSetup,
            Response = metadata
        });
    }


    private async Task LoadGameVersionsAsync(ModLoaderType loader) {
        _loaderManifestVersions = await ModrinthApiService.GetGameVersionsAsync(SelectedLoader ?? loader);

        var gameVersions =
            _loaderManifestVersions.Where(v => !v.Id.StartsWith('$'))
                .Select(v => v.Id);
            
        GameVersions.Clear();
        GameVersions.AddRange(gameVersions.Reverse());
        SelectedGameVersion = GameVersions.FirstOrDefault();
    }

    private void LoadLoaderVersions(string? _) {
        if (_loaderManifestVersions == null ||
            SelectedLoader == null ||
            SelectedGameVersion == null)
            return;
        
        LoaderVersions.Clear();
        LoaderVersions.AddRange(_loaderManifestVersions
            .First(v => v.Id == SelectedGameVersion)
            .Loaders
            .Select(lv => lv.Id));

        SelectedLoaderVersion = "";
        SelectedLoaderVersion = LoaderVersions.FirstOrDefault();
    }
    
    private static int CompareMcVersions( string a, string b ) {
        var va = ParseMcVersion(a);
        var vb = ParseMcVersion(b);

        var len = Math.Max(va.Length, vb.Length);

        for(int i = 0; i < len; i++) {
            var pa = i < va.Length ? va[i] : 0;
            var pb = i < vb.Length ? vb[i] : 0;

            var cmp = pa.CompareTo(pb);
            if(cmp != 0)
                return cmp;
        }

        return 0;
    }

    private static int[] ParseMcVersion( string version ) {
        // elimină snapshot-uri / sufixe
        var clean = version.Split('-')[0];

        return clean
            .Split('.')
            .Select(p => int.TryParse(p, out var n) ? n : 0)
            .ToArray();
    }
}

public enum CreateModpackDialogResult {
    Cancel,
    CustomSetup,
    InstallModpack,
    ImportInstance
}