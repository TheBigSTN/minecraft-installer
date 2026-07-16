using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Reactive;
using System.Threading.Tasks;
using DynamicData;
using ModpackInstaller.Models;
using ModpackInstaller.Services.Modpack;
using ReactiveUI;
using ReactiveUI.SourceGenerators;

namespace ModpackInstaller.ViewModels.ModpackBody;

public partial class ContentViewModel : ViewModelBase {
    private readonly ModpackMetadata _modpackMetadata;

    [Reactive] private string _searchQuery= "";

    public ReactiveCommand<ModInfo, Unit> RemoveModCommand { get; }
    public ReactiveCommand<ModInfo, Unit> ToggleModCommand { get; }
    public ReactiveCommand<Unit, Unit> BrowseContentCommand { get; }

    [Reactive]
    private ObservableCollection<ModInfo> _mods = [];
    
    private readonly ObservableCollection<ModInfo> _allMods = [];

    public ContentViewModel(MainViewModel main, ModpackMetadata modpackMetadata) {
        _modpackMetadata = modpackMetadata;
        var manifestService = ModpackManifestService.CreateInstance(modpackMetadata.InstallPath);
        
        _allMods.AddRange(manifestService.InstalledMods.ToList());
        
        this.WhenAnyValue(x => x.SearchQuery)
            .Subscribe(_ => FilterMods());

        manifestService.InstalledMods.Changed += () => {
            _allMods.Clear();
            _allMods.AddRange(manifestService.InstalledMods.ToList());
            FilterMods();
        };
        FilterMods();
        
        RemoveModCommand = ReactiveCommand.Create<ModInfo>(modInfo => {
            manifestService.RemoveMod(modInfo);

            _mods.Remove(modInfo);
        });
        
        ToggleModCommand = ReactiveCommand.Create<ModInfo>(modInfo => {
            manifestService.EnableDisableMod(modInfo.ProjectId, !modInfo.Enabled);
        });
        
        BrowseContentCommand = ReactiveCommand.Create(() => {
            main.OpenDiscovery(modpackMetadata);
        });

        _ = Task.Run(async () => {
            await manifestService.SyncToFileSistemAsync(modpackMetadata.IsServerInstall);
            await manifestService.SyncWithFilesystemAsync();
            FilterMods();
        });
    }
    private void FilterMods() {
        Mods.Clear();

        if (string.IsNullOrWhiteSpace(SearchQuery))
        {
            foreach (var mod in _allMods) Mods.Add(mod);
            return;
        }

        // 1. Spargem query-ul în cuvinte (tokeni) și eliminăm spațiile goale
        var queryTokens = SearchQuery.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        var filtered = _allMods.Where(m =>
        {
            // 2. Verificăm dacă TOȚI tokenii se găsesc în Title SAU OwnerName
            // .All verifică dacă fiecare cuvânt din search se regăsește undeva
            return queryTokens.All(token => 
                m.Title.Contains(token, StringComparison.OrdinalIgnoreCase) ||
                m.OwnerName.Contains(token, StringComparison.OrdinalIgnoreCase));
        });

        foreach (var mod in filtered)
            Mods.Add(mod);
    }
}