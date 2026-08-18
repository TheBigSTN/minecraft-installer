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
using ModpackInstaller.Models.Interfaces;
using ModpackInstaller.Models.Modrinth;
using ModpackInstaller.Services;
using ModpackInstaller.Services.Modpack;
using ModpackInstaller.ViewModels.Dialogs;
using ReactiveUI;
using ReactiveUI.SourceGenerators;

namespace ModpackInstaller.ViewModels.Body;

public partial class DiscoveryPageViewModel : ViewModelBase {
    private readonly MainViewModel _main;
    private ModpackManifestStorage? _store;

    [Reactive] private DiscoveryCategory _selectedCategory;

    [Reactive] private IReadOnlyModpackMetadata? _modpackMetadata;

    [Reactive] private string _searchQuery;

    [Reactive] private InstallPlatform _selectedModpackLoader;
    public ObservableCollection<DiscoveryItem> DiscoveryItems { get; } = [];

    private readonly ObservableCollection<DiscoveryItem> _allDiscoveryItems = [];
    public ReactiveCommand<DiscoveryItem, Unit> OpenItemCommand { get; }

    public DiscoveryPageViewModel(MainViewModel main) {
        _searchQuery = "";
        _main = main;
        _selectedCategory = DiscoveryCategory.Modpacks;
        var loadCommand = ReactiveCommand.CreateFromTask<DiscoveryCategory>(LoadAsync);

        this.WhenAnyValue(x => x.SelectedCategory)
            .Throttle(TimeSpan.FromMilliseconds(300))
            .ObserveOn(RxApp.MainThreadScheduler)
            .InvokeCommand(loadCommand);

        this.WhenAnyValue(x => x.SearchQuery)
            .Skip(1)
            .Throttle(TimeSpan.FromMilliseconds(300))
            .Select(_ => SelectedCategory)
            .ObserveOn(RxApp.MainThreadScheduler)
            .InvokeCommand(loadCommand);

        this.WhenAnyValue(t => t.SelectedModpackLoader)
            .Skip(1)
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(loader => { AppSettings.Settings.SetInstallTarget(loader); });

        OpenItemCommand = ReactiveCommand.CreateFromTask<DiscoveryItem>(OpenItemAsync);
    }

    private async Task OpenItemAsync(DiscoveryItem item) {
        switch (SelectedCategory) {
            case DiscoveryCategory.Modpacks:
                if (item.Source is PublicModpackRequestResponse) {
                    var response = await _main.ShowDialogAsync(
                        await ModpackSelectVersionForInstallDialogViewModel
                              .CreateInstance(Guid.Parse(item.Id)).ConfigureAwait(false)
                    ).ConfigureAwait(false);

                    if (response is null) return;

                    await InstallItemAsync(item, response.Id, response.Semver).ConfigureAwait(false);
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

    private DiscoveryPageViewModel(MainViewModel main, ModpackMetadataStorage modpackMetadataStorage) : this(main) {
        _modpackMetadata = modpackMetadataStorage.GetData();
        SelectedCategory = DiscoveryCategory.Mods;
    }

    public static async Task<DiscoveryPageViewModel> CreateInstanceAsync(
        MainViewModel main,
        ModpackMetadataStorage modpackMetadataStorage) {
        var data = new DiscoveryPageViewModel(main, modpackMetadataStorage) {
            _store = await ModpackManifestStorage
                           .CreateInstanceAsync(modpackMetadataStorage.GetData().InstallPath)
                           .ConfigureAwait(false)
        };

        return data;
    }

    private async Task<Unit> LoadAsync(DiscoveryCategory discoveryCategory) {
        _allDiscoveryItems.Clear();
        switch (SelectedCategory) {
            case DiscoveryCategory.Modpacks:
                var results = await BackendApiService.GetPublicModpacksAsync().ConfigureAwait(false);

                foreach (var item in results.Select(result => new DiscoveryItem {
                             Id = result.Id,
                             AuthorName = result.AuthorName,
                             Description = result.Description,
                             Title = result.ModpackName,
                             Source = result,
                             IsInstalled = false,
                             IsInstalling = false,
                             InstallCommand =
                                 ReactiveCommand
                                     .CreateFromTask<
                                         DiscoveryItem>(item =>
                                                            InstallItemAsync(item))
                         })) {
                    _allDiscoveryItems.Add(item);
                }

                break;
            case DiscoveryCategory.Mods:
                if (_modpackMetadata == null ||
                    _store == null) return Unit.Default;

                var mods = await ModrinthApiService.SearchOnModrinthAsync(SearchQuery, _modpackMetadata, 0)
                                                   .ConfigureAwait(false);

                mods.ForEach(mod => {
                    _allDiscoveryItems.Add(new DiscoveryItem {
                        Id = mod.Id,
                        AuthorName = mod.Author,
                        Description = mod.Description ?? "",
                        Title = mod.Title ?? "",
                        Source = mod,
                        IsInstalled =
                            _store.StateService.IsModInstalled(
                                new ModVersion(mod.Id, "")),
                        IsInstalling = false,
                        InstallCommand =
                            ReactiveCommand
                                .CreateFromTask<
                                    DiscoveryItem>(item => InstallItemAsync(item))
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

    private async Task InstallItemAsync(DiscoveryItem item, Guid? versionId = null, string? versionSemver = null) {
        item.IsInstalling = true;
        try {
            switch (SelectedCategory) {
                case DiscoveryCategory.Modpacks: {
                    if (item.Source is PublicModpackRequestResponse itemSource) {
                        var exists = ModpackMetadataRegistry.Exists(Guid.Parse(item.Id));

                        if (exists) {
                            var shouldDuplicate =
                                await _main.ShowDialogAsync(new ModpackConflictDialogViewModel(item.Title));

                            if (!shouldDuplicate)
                                return;
                        }

                        var existingNames = ModpackMetadataRegistry.LoadAll()
                                                                   .Select(x => x.Name.Trim())
                                                                   .ToHashSet(StringComparer.OrdinalIgnoreCase);

                        var originalName = itemSource.ModpackName.Trim();
                        var name = originalName;
                        var index = 1;

                        while (existingNames.Contains(name)) {
                            name = $"{originalName} ({index++})";
                        }

                        itemSource.ModpackName = name;

                        var response = await _main.ShowDialogAsync(new ManualSetupDialogViewModel(itemSource))
                                                  .ConfigureAwait(false);

                        if (response is not ModpackManualSetupResponse.Finished) return;

                        if (versionId is not null && versionSemver is not null) {
                            itemSource.LatestVersionId = versionId.Value;
                            itemSource.LatestVersion = versionSemver;
                        }

                        var metadata = await ModpackInstallService.DownloadAndInstallModpackAsync(
                            itemSource,
                            AppVariables.GetBaseInstallPathFromLauncer(AppSettings.Settings.Config.InstallTarget));

                        await _main.OpenModpackAsync(metadata).ConfigureAwait(false);
                    }

                    break;
                }
                case DiscoveryCategory.Mods: {
                    if (_modpackMetadata == null ||
                        _store == null) return;

                    if (item.Source is ModrinthSearchProject itemSource) {
                        var version = await ModrinthApiService.GetCompatibleVersionAsync(
                            itemSource.Id,
                            _modpackMetadata.GameVersion,
                            _modpackMetadata.Loader
                        ).ConfigureAwait(false);

                        await _store.InstallationManager
                                    .InstallModAsync(new ModVersion(itemSource.Id, version!.Id), false)
                                    .ConfigureAwait(false);

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