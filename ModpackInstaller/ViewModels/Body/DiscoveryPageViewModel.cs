using System;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using DynamicData;
using ModpackInstaller.Infrastructure;
using ModpackInstaller.Models;
using ModpackInstaller.Models.DTOs;
using ModpackInstaller.Models.Modrinth;
using ModpackInstaller.Services;
using ModpackInstaller.Services.Modpack;
using ModpackInstaller.ViewModels.Dialogs;
using ReactiveUI;
using ReactiveUI.SourceGenerators;

namespace ModpackInstaller.ViewModels.Body;

public partial class DiscoveryPageViewModel : ViewModelBase {
    private readonly MainViewModel _main;
    private readonly ModpackManifestService? _modpackManifestService;

    [Reactive] private DiscoveryCategory _selectedCategory;

    [Reactive] private ModpackMetadata? _modpackMetadata;

    [Reactive] private string _searchQuerry;

    [Reactive] private InstallPlatform _selectedModpackLoader;
    public ObservableCollection<DiscoveryItem> DiscoveryItems { get; } = [];

    private readonly ObservableCollection<DiscoveryItem> _allDiscoveryItems = [];
    public ReactiveCommand<DiscoveryItem, Unit> OpenItemCommand { get; }

    public DiscoveryPageViewModel(MainViewModel main) {
        _searchQuerry = "";
        _main = main;
        _selectedCategory = DiscoveryCategory.Modpacks;
        var loadCommand = ReactiveCommand.CreateFromTask<DiscoveryCategory>(AsyncLoad);

        this.WhenAnyValue(x => x.SelectedCategory)
            .Throttle(TimeSpan.FromMilliseconds(300))
            .ObserveOn(RxApp.MainThreadScheduler)
            .InvokeCommand(loadCommand);

        this.WhenAnyValue(x => x.SearchQuerry)
            .Skip(1)
            .Throttle(TimeSpan.FromMilliseconds(300))
            .Select(_ => SelectedCategory)
            .ObserveOn(RxApp.MainThreadScheduler)
            .InvokeCommand(loadCommand);

        this.WhenAnyValue(t => t.SelectedModpackLoader)
            .Skip(1)
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(loader => { AppSettings.Settings.SetInstallTarget(loader); });

        OpenItemCommand = ReactiveCommand.CreateFromTask<DiscoveryItem>(OpenItem);
    }

    private async Task OpenItem(DiscoveryItem item) {
        switch (SelectedCategory) {
            case DiscoveryCategory.Modpacks:
                if (item.Source is PublicModpackRequestResponse itemSource) {
                    var response = await _main.ShowDialog(
                        await ModpackSelectVersionForInstallDialogViewModel
                            .CreateInstance(Guid.Parse(item.Id))
                    );

                    if (response is null) return;

                    await InstallItem(item, response.Id, response.Semver);
                }

                break;
            case DiscoveryCategory.Mods:
            case DiscoveryCategory.ResourcePacks:
            case DiscoveryCategory.DataPacks:
            case DiscoveryCategory.Shaders:
            default:
                break;
        }
    }

    public DiscoveryPageViewModel(MainViewModel main, ModpackMetadata modpackMetadata) : this(main) {
        _modpackMetadata = modpackMetadata;
        _modpackManifestService = ModpackManifestService.CreateInstance(modpackMetadata.InstallPath);
        SelectedCategory = DiscoveryCategory.Mods;
    }

    private async Task<Unit> AsyncLoad(DiscoveryCategory discoveryCategory) {
        _allDiscoveryItems.Clear();
        switch (SelectedCategory) {
            case DiscoveryCategory.Modpacks:
                var results = await BackendApiService.GetPublicModpacksAsync();

                foreach (var item in results.Select(result => new DiscoveryItem {
                             Id = result.Id,
                             AuthorName = result.AuthorName,
                             Description = result.Description,
                             Title = result.ModpackName,
                             Source = result,
                             IsInstalled = false,
                             IsInstalling = false,
                             InstallCommand = ReactiveCommand.CreateFromTask<DiscoveryItem>(async item => await InstallItem(item))
                         })) {
                    _allDiscoveryItems.Add(item);
                }

                break;
            case DiscoveryCategory.Mods:
                if (_modpackMetadata == null ||
                    _modpackManifestService == null) return Unit.Default;

                var mods = await ModrinthApiService.SearchOnModrinthAsync(SearchQuerry, _modpackMetadata, 0);

                mods.ForEach(mod => {
                    _allDiscoveryItems.Add(new DiscoveryItem {
                        Id = mod.Id,
                        AuthorName = mod.Author,
                        Description = mod.Description ?? "",
                        Title = mod.Title ?? "",
                        Source = mod,
                        IsInstalled =
                            _modpackManifestService.IsModInstalled(new ModInfo { ProjectId = mod.Id, VersionId = "" }),
                        IsInstalling = false,
                        InstallCommand = ReactiveCommand.CreateFromTask<DiscoveryItem>(async item => await InstallItem(item))
                    });
                });

                break;
            case DiscoveryCategory.DataPacks:
            case DiscoveryCategory.ResourcePacks:
            case DiscoveryCategory.Shaders:
            default:
                break;
        }

        DiscoveryItems.Clear();
        DiscoveryItems.AddRange(_allDiscoveryItems);
        return Unit.Default;
    }

    private async Task InstallItem(DiscoveryItem item, Guid? versionId = null, string? versionSemver = null) {
        item.IsInstalling = true;
        try {
            switch (SelectedCategory) {
                case DiscoveryCategory.Modpacks: {
                    if (item.Source is PublicModpackRequestResponse itemSource) {
                        var exists = ModpackMedatataService.ExistsModpack(Guid.Parse(item.Id));

                        if (exists) {
                            var shouldDuplicate =
                                await _main.ShowDialog(new ModpackConflictDialogViewModel(item.Title));

                            if (!shouldDuplicate)
                                return;
                        }

                        var existingNames = ModpackMedatataService.LoadAll()
                            .Select(x => x.Name.Trim())
                            .ToHashSet(StringComparer.OrdinalIgnoreCase);

                        var originalName = itemSource.ModpackName.Trim();
                        var name = originalName;
                        var index = 1;

                        while (existingNames.Contains(name)) {
                            name = $"{originalName} ({index++})";
                        }

                        itemSource.ModpackName = name;

                        var response = await _main.ShowDialog(new ManualSetupDialogViewModel(itemSource));

                        if (response is not ModpackManualSetupResponse.Finished) return;

                        if (versionId is not null && versionSemver is not null) {
                            itemSource.LatestVersionId = versionId.Value;
                            itemSource.LatestVersion = versionSemver;
                        }

                        var metadata = await ModpackInstallService.DownloadAndInstallModpack(
                            itemSource,
                            AppVariables.GetBaseInstallPathFromLauncer(AppSettings.Settings.Config.InstallTarget));

                        _main.OpenModpack(metadata);
                    }

                    break;
                }
                case DiscoveryCategory.Mods: {
                    if (_modpackMetadata == null ||
                        _modpackManifestService == null) return;

                    if (item.Source is ModrinthSearchProject itemSource) {
                        var version = await ModrinthApiService.GetCompatibleVersionAsync(
                            itemSource.Id,
                            _modpackMetadata.GameVersion,
                            _modpackMetadata.Loader
                        );

                        await _modpackManifestService.InstallModAsync(new ModVersion(itemSource.Id, version!.Id),
                            false);

                        item.IsInstalled = true;
                        this.RaisePropertyChanged(nameof(item.IsInstalled));
                    }
                    break;
                }
                case DiscoveryCategory.DataPacks:
                case DiscoveryCategory.ResourcePacks:
                case DiscoveryCategory.Shaders:
                default:
                    break;
            }
        }
        finally {
            item.IsInstalling = false;
        }
    }
}

public enum DiscoveryCategory {
    Modpacks,
    Mods,
    ResourcePacks,
    DataPacks,
    Shaders
}