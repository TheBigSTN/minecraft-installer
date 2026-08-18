using System.Diagnostics;
using System.Reactive;
using System.Threading.Tasks;
using ModpackInstaller.Models.Backend;
using ModpackInstaller.Models.Interfaces;
using ModpackInstaller.Services;
using ModpackInstaller.Services.Modpack;
using ModpackInstaller.ViewModels.Dialogs;
using ModpackInstaller.ViewModels.ModpackBody;
using ReactiveUI;
using ReactiveUI.SourceGenerators;

namespace ModpackInstaller.ViewModels.Body;

public partial class ModpackPageViewModel : ViewModelBase {
    private readonly IReadOnlyModpackMetadata _metadata;
    
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

    private ModpackPageViewModel(MainViewModel main, 
                                 ModpackMetadataStorage metadataStorage,
                                 ModpackManifestStorage manifestStorage) {
        _metadata = metadataStorage.GetData();
        BodyViewModel = new ContentViewModel(main, metadataStorage, manifestStorage);
        
        ContentPageCommand = ReactiveCommand.Create(() => {
            BodyViewModel = new ContentViewModel(main, metadataStorage, manifestStorage);
        });
        
        FilesPageCommand = ReactiveCommand.Create(() => {
            BodyViewModel = new FilesViewModel(_metadata.InstallPath);
        });
        
        ConfigPageCommand = ReactiveCommand.Create(() => {

        });
        
        OpenDirectoryCommand = ReactiveCommand.Create(() => {
            Process.Start(new ProcessStartInfo {
                FileName = _metadata.InstallPath,
                UseShellExecute = true
            });
        });
        
        SettingsPopUpCommand = ReactiveCommand.CreateFromTask(async () => {
            _ = await main.ShowDialogAsync(
                            await ModpackSettingsDialogViewModel
                                  .CreateInstanceAsync(main, metadataStorage, manifestStorage)
                                  .ConfigureAwait(false))
                          .ConfigureAwait(false);
        });
        
        UpdateModpackCommand = ReactiveCommand.CreateFromTask(async () => {
            if (RemoteModpack is null)
                return;
            
            var updatedMetadata = await ModpackInstallService.UpdateModpackAsync(metadataStorage).ConfigureAwait(false);

            if (!updatedMetadata) {
                main.OpenHome();
                return;
            }
            
            this.RaisePropertyChanged(nameof(IsLatestVersion));
        });
    }
    
    

    public static async Task<ModpackPageViewModel> CreateInstanceAsync(MainViewModel main, ModpackMetadataStorage metadataStorage) {
        var manifestStorage = await ModpackManifestStorage.CreateInstanceAsync(metadataStorage.GetData().InstallPath).ConfigureAwait(false);
        var sw = Stopwatch.StartNew();
        ModpackPageViewModel modpackPageViewModel = new(main, metadataStorage, manifestStorage);
        await modpackPageViewModel.LoadRemoteModpackAsync().ConfigureAwait(true);
        Debug.WriteLine($"LoadRemoteModpackAsync: {sw.ElapsedMilliseconds} ms");
        return modpackPageViewModel;
    }

    private async Task LoadRemoteModpackAsync() {
        if (_metadata.ModpackId is null)
            return;

        try {
            RemoteModpack = await BackendApiService.GetModpack(_metadata.ModpackId.Value).ConfigureAwait(false);
        } catch {
            RemoteModpack = null;
        }

        this.RaisePropertyChanged(nameof(IsLatestVersion));
    }
}