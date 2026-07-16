using System;
using System.Collections.ObjectModel;
using System.Reactive;
using System.Threading.Tasks;
using DynamicData;
using ModpackInstaller.Models;
using ModpackInstaller.Models.Backend;
using ModpackInstaller.Models.DTOs;
using ModpackInstaller.Services;
using ModpackInstaller.Services.Modpack;
using ReactiveUI;
using ReactiveUI.SourceGenerators;

namespace ModpackInstaller.ViewModels.Dialogs;

public partial class ModpackSettingsDialogViewModel : DialogViewModel<Unit> {
    private readonly ModpackMetadata _metadata;

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
        ModpackMetadata metadata) {
        _metadata = metadata;
        ModpackPublicizeService modpackPublicize = new(metadata);

        CloseCommand = ReactiveCommand.Create(() => Close(Unit.Default));

        PublishModpackCommand = ReactiveCommand.CreateFromTask(async () => {
            if (_metadata.ModpackId is not null)
                return;

            await modpackPublicize.CreateOnServerAsync(false);
            
            await LoadModpackInfo();
            await LoadVersions();

            this.RaisePropertyChanged(nameof(IsPublished));
            this.RaisePropertyChanged(nameof(IsOwnedByYou));
        });

        CreateNewVersionCommand = ReactiveCommand.CreateFromTask(async () => {
            await main.ShowDialog(new CreateModpackUpdateDialogViewModel(metadata));

            await LoadVersions();
        });

        DeleteModpackCommand = ReactiveCommand.Create(() => {
            var successful = new ModpackMedatataService().Delete(metadata.Id, out _);

            if (successful) {
                main.OpenHome();
                // var instance = ModpackManifestService.CreateInstance(metadata.InstallPath);
                ModpackManifestService.ReleaseInstance(metadata.InstallPath);
            }

            Close(Unit.Default);
        });
    }

    public static async Task<ModpackSettingsDialogViewModel> CreateInstance(
        MainViewModel main,
        ModpackMetadata metadata) {
        var vm = new ModpackSettingsDialogViewModel(main, metadata);

        await vm.LoadVersions();
        await vm.LoadModpackInfo();

        return vm;
    }

    [Reactive] private ModpackDto? _remoteModpackInfo;

    private async Task LoadModpackInfo() {
        if (_metadata.ModpackId is null) return;
        
        _remoteModpackInfo = await BackendApiService.GetModpack(_metadata.ModpackId.Value);

        IsPublic = _remoteModpackInfo is not null;
    }


    private async Task LoadVersions() {
        if (_metadata.ModpackId is null) return;
        var versions = await BackendApiService.GetModpackVersionsAsync(_metadata.ModpackId.Value);
        ModpackVersions.Clear();
        ModpackVersions.AddRange(versions.ConvertAll(i => new ModpackVersionItemViewModel(i)));
    }
}