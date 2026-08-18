using System;
using System.Collections.ObjectModel;
using System.Reactive;
using System.Threading.Tasks;
using DynamicData;
using ModpackInstaller.Models;
using ModpackInstaller.Models.Backend;
using ModpackInstaller.Models.Interfaces;
using ModpackInstaller.Services;
using ModpackInstaller.Services.Modpack;
using ReactiveUI;
using ReactiveUI.SourceGenerators;

namespace ModpackInstaller.ViewModels.Dialogs;

public partial class ModpackSettingsDialogViewModel : DialogViewModel<Unit> {
    private readonly IReadOnlyModpackMetadata _metadata;

    [Reactive] private bool _isPublic;

    [Reactive] private bool _isCodeShared;

    public bool IsPublished => !string.IsNullOrEmpty(_metadata.ModpackPassword);
    
    public bool IsOwnedByYou => !IsPublished ||
                                AppSettings.Settings.Config.UserId == RemoteModpackInfo?.Owner.Id &&
                                !string.IsNullOrEmpty(_metadata.ModpackPassword);

    public ReactiveCommand<Unit, Unit> CloseCommand { get; }
    public ReactiveCommand<Unit, Unit> PublishModpackCommand { get; }
    public ReactiveCommand<Unit, Unit> CreateNewVersionCommand { get; }

    public ReactiveCommand<Unit, Unit> DeleteModpackCommand { get; }
    public ObservableCollection<ModpackVersionItemViewModel> ModpackVersions { get; } = [];

    private ModpackSettingsDialogViewModel(
        MainViewModel main,
        ModpackMetadataStorage metadataStorage,
        ModpackManifestStorage manifestStorage) {
        _metadata = metadataStorage.GetData();
        ModpackPublicizeService modpackPublicize = new(metadataStorage);

        CloseCommand = ReactiveCommand.Create(() => Close(Unit.Default));

        PublishModpackCommand = ReactiveCommand.CreateFromTask(async () => {
            if (_metadata.ModpackId is not null)
                return;

            await modpackPublicize.CreateOnServerAsync(false).ConfigureAwait(false);
            
            await LoadModpackInfoAsync().ConfigureAwait(false);
            await LoadVersionsAsync().ConfigureAwait(false);

            this.RaisePropertyChanged(nameof(IsPublished));
            this.RaisePropertyChanged(nameof(IsOwnedByYou));
        });

        CreateNewVersionCommand = ReactiveCommand.CreateFromTask(async () => {
            await main.ShowDialogAsync(new CreateModpackUpdateDialogViewModel(metadataStorage, manifestStorage)).ConfigureAwait(false);

            await LoadVersionsAsync().ConfigureAwait(false);
        });

        DeleteModpackCommand = ReactiveCommand.Create(() => {
            var successful = ModpackMetadataRegistry.Delete(_metadata.Id, out _);

            if (successful) {
                main.OpenHome();
                // var instance = ModpackManifestService.CreateInstance(metadata.InstallPath);
                ModpackManifestStorage.ReleaseInstance(_metadata.InstallPath);
            }

            Close(Unit.Default);
        });
    }

    public static async Task<ModpackSettingsDialogViewModel> CreateInstanceAsync(
        MainViewModel main,
        ModpackMetadataStorage metadataStorage,
        ModpackManifestStorage manifestStorage) {
        var vm = new ModpackSettingsDialogViewModel(main, metadataStorage, manifestStorage);

        await vm.LoadVersionsAsync().ConfigureAwait(false);
        await vm.LoadModpackInfoAsync().ConfigureAwait(false);

        return vm;
    }

    [Reactive] private ModpackDto? _remoteModpackInfo;

    private async Task LoadModpackInfoAsync() {
        if (_metadata.ModpackId is null) return;
        
        _remoteModpackInfo = await BackendApiService.GetModpack(_metadata.ModpackId.Value).ConfigureAwait(false);

        IsPublic = _remoteModpackInfo is not null;
    }


    private async Task LoadVersionsAsync() {
        if (_metadata.ModpackId is null) return;
        var versions = await BackendApiService.GetModpackVersionsAsync(_metadata.ModpackId.Value).ConfigureAwait(false);
        ModpackVersions.Clear();
        ModpackVersions.AddRange(versions.ConvertAll(i => new ModpackVersionItemViewModel(i)));
    }
}