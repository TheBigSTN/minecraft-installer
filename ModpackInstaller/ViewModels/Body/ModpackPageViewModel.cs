using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reactive;
using System.Threading.Tasks;
using ModpackInstaller.Models;
using ModpackInstaller.Models.Backend;
using ModpackInstaller.Services;
using ModpackInstaller.Services.Modpack;
using ModpackInstaller.ViewModels.Dialogs;
using ModpackInstaller.ViewModels.ModpackBody;
using ReactiveUI;
using ReactiveUI.SourceGenerators;

namespace ModpackInstaller.ViewModels.Body;

public partial class ModpackPageViewModel : ViewModelBase {
    private ModpackMetadata _metadata;
    
    [Reactive] private ModpackDto? _remoteModpack;
    
    
    public string Name => _metadata.Name;
    public string GameVersion => _metadata.GameVersion;
    public string Loader => _metadata.Loader.ToString();
    public bool IsLatestVersion =>
        _metadata.ModpackPassword is not null ||
        _remoteModpack is null ||
        _metadata.ModpackId is null ||
        _metadata.VersionSemver == _remoteModpack.LatestVersion.Semver;
    
    [Reactive]
    private ViewModelBase _bodyViewModel;
    public ReactiveCommand<Unit, Unit> ContentPageCommand { get; }
    public ReactiveCommand<Unit, Unit> FilesPageCommand { get; }
    public ReactiveCommand<Unit, Unit> OpenDirectoryCommand { get; }
    public ReactiveCommand<Unit, Unit> ConfigPageCommand { get; }
    public ReactiveCommand<Unit, Unit> UpdateModpackCommand { get; }
    public ReactiveCommand<Unit, Unit> SettingsPopUpCommand { get; }
    
    public ModpackPageViewModel(MainViewModel main, ModpackMetadata metadata) {
        _metadata = metadata;
        BodyViewModel = new ContentViewModel(main, metadata);
        _ = LoadRemoteModpack();
        
        ContentPageCommand = ReactiveCommand.Create(() => {
            BodyViewModel = new ContentViewModel(main, metadata);
        });
        
        FilesPageCommand = ReactiveCommand.Create(() => {

        });
        
        ConfigPageCommand = ReactiveCommand.Create(() => {

        });
        
        OpenDirectoryCommand = ReactiveCommand.Create(() => {
            Process.Start(new ProcessStartInfo {
                FileName = metadata.InstallPath,
                UseShellExecute = true
            });
        });
        
        SettingsPopUpCommand = ReactiveCommand.CreateFromTask(async () => {
            _ = await main.ShowDialog(await ModpackSettingsDialogViewModel.CreateInstance(main, _metadata));
        });
        
        UpdateModpackCommand = ReactiveCommand.CreateFromTask(async () => {
            if (RemoteModpack is null)
                return;
            
            var updatedMetadata = await ModpackInstallService.UpdateModpack(_metadata);

            if (updatedMetadata == null) {
                main.OpenHome();
                return;
            }

            _metadata = updatedMetadata;
            this.RaisePropertyChanged(nameof(IsLatestVersion));
        });
    }
    
    private async Task LoadRemoteModpack() {
        if (_metadata.ModpackId is null)
            return;

        RemoteModpack = await BackendApiService.GetModpack(_metadata.ModpackId.Value);
        this.RaisePropertyChanged(nameof(IsLatestVersion));
    }
}