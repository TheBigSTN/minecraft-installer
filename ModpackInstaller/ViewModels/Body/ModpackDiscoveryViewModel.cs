using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Text;
using System.Threading.Tasks;
using ModpackInstaller.Services;
using ReactiveUI;

namespace ModpackInstaller.ViewModels.Body;

public class ModpackDiscoveryViewModel : ViewModelBase {
    public ObservableCollection<ModpackResponse> DiscoveryModpacks { get; } = new();
    public ReactiveCommand<Unit, Unit> RefreshCommand { get; }
    public ReactiveCommand<ModpackResponse, Unit> InstallModpackCommand { get; }

    public ModpackDiscoveryViewModel() {
        RefreshCommand = ReactiveCommand.CreateFromTask(LoadModpacksAsync);

        InstallModpackCommand = ReactiveCommand.CreateFromTask<ModpackResponse>(async (modpack) => {
            // Logica ta de instalare: Download ZIP -> Extract -> Save Local
            Console.WriteLine($"Installing modpack: {modpack.Name}");
        });

        // Încărcăm datele la inițializare
        LoadModpacksAsync();
    }

    private async Task LoadModpacksAsync() {
        try {
            // Presupunem că ai un endpoint public /api/v1/modpacks/discovery sau similar
            //var results = await ModpackApiService.();
            //DiscoveryModpacks.Clear();
            //if (results != null) {
            //    foreach (var mp in results) DiscoveryModpacks.Add(mp);
            //}
        }
        catch (Exception ex) {
            Console.WriteLine($"Error loading discovery: {ex.Message}");
        }
    }
}