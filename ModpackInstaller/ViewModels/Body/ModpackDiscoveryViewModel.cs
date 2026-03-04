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
    private readonly IDialogService _dialogService;

    public ObservableCollection<PublicModpackRequestResponse> DiscoveryModpacks { get; } = new();
    public ReactiveCommand<Unit, Unit> RefreshCommand { get; }
    public ReactiveCommand<PublicModpackRequestResponse, Unit> InstallModpackCommand { get; }

    public ModpackDiscoveryViewModel(MainViewModel main, IDialogService dialogService) {

        _main = main;
        _dialogService = dialogService;

        RefreshCommand = ReactiveCommand.CreateFromTask(LoadModpacksAsync);

        InstallModpackCommand = ReactiveCommand.CreateFromTask<PublicModpackRequestResponse>(async (modpack) => {
            if (modpack is null) return;
            ModpackMetadata manifest = await ModpackInstallService.DownloadAndInstallModpack(modpack, _main.InstallPath);

            _main.RefreshModpackList();

            await _dialogService.EmitSimpleOkDialog("Install Complete", $"The modpack {manifest.Name} was installed successfully");
        });

        // Încărcăm datele la inițializare
        _ = LoadModpacksAsync();
    }

    private async Task LoadModpacksAsync() {
        try {
            // Presupunem că ai un endpoint public /api/v1/modpacks/discovery sau similar
            var results = await ModpackApiService.GetPublicModpacksAsync();
            DiscoveryModpacks.Clear();
            if (results != null) {
                foreach (var mp in results) DiscoveryModpacks.Add(mp);
            }
        }
        catch (Exception ex) {
            Console.WriteLine($"Error loading discovery: {ex.Message}");
            await _dialogService.EmitSimpleOkDialog("Error", $"The modpack failed to instal.\nReason: {ex.Message}.");
        }
    }
}