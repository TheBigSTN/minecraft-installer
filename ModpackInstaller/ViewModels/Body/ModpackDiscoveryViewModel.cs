using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO.Compression;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Text;
using System.Threading.Tasks;
using ModpackInstaller.Models;
using ModpackInstaller.Services;
using ReactiveUI;
using ModpackInstaller.Infrastructure;
using ModpackInstaller.Services.Modpack;
using ModpackInstaller.Models.DTOs;

namespace ModpackInstaller.ViewModels.Body;

public class ModpackDiscoveryViewModel : ViewModelBase {
    private readonly MainViewModel _main;

    public ObservableCollection<DiscoveryModpackDisplay> DiscoveryModpacks { get; } = new();
    public ReactiveCommand<Unit, Unit> RefreshCommand { get; }
    public ReactiveCommand<DiscoveryModpackDisplay, Unit> InstallModpackCommand { get; }

    public ModpackDiscoveryViewModel(MainViewModel main) {

        _main = main;

        RefreshCommand = ReactiveCommand.CreateFromTask(LoadModpacksAsync);

        InstallModpackCommand = ReactiveCommand.CreateFromTask<DiscoveryModpackDisplay>(async (modpack) => {
            if (modpack is null) return;

            _main.ProgressText = "Se instalează modpack-ul...";
            _main.InstallProgress = 0;
            _main.IsProgressIndeterminate = true;
            _main.IsGlobalBusy = true;

            var progressHandler = new Progress<double>(value => {
                _main.IsProgressIndeterminate = false;
                _main.InstallProgress = value;
            });

            Action<string> updateStatusText = ( text ) => {
                _main.ProgressText = text;
            };

            try {
                ModpackMetadata manifest = await ModpackInstallService.DownloadAndInstallModpack(modpack, _main.InstallPath, progressHandler, updateStatusText);
                _main.RefreshModpackList();
                _main.ShowDiscovery();
            } catch(Exception ex) {
                await _main.DialogService.EmitSimpleOkDialog("Eroare", ex.Message);
            } finally {
                _main.IsGlobalBusy = false;
                _main.ProgressText = "";
            }

        });

        // Încărcăm datele la inițializare
        _ = LoadModpacksAsync();
    }

    private async Task LoadModpacksAsync() {
        try {
            // Presupunem că ai un endpoint public /api/v1/modpacks/discovery sau similar
            var results = await BackendApiService.GetPublicModpacksAsync();
            DiscoveryModpacks.Clear();
            if (results != null) {
                foreach(var mp in results) DiscoveryModpacks.Add(new DiscoveryModpackDisplay(mp, CanInstall(mp)));
            }
        }
        catch (Exception ex) {
            Console.WriteLine($"Error loading discovery: {ex.Message}");
            await _main.DialogService.EmitSimpleOkDialog("Error", $"The modpack failed to instal.\nReason: {ex.Message}.");
        }
    }

    public bool CanInstall( PublicModpackRequestResponse modpack ) {
        if(modpack == null)
            return false;
        return !_main.modpackMedatataService.Exists(modpack.Id);
    }

}