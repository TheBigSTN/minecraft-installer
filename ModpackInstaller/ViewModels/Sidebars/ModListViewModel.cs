using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Threading;
using ModpackInstaller.Models;
using ModpackInstaller.Services.Modpack;
using ReactiveUI;

namespace ModpackInstaller.ViewModels.Sidebars;

public class ModListViewModel : ViewModelBase {
    private readonly MainViewModel _main;
    public ObservableCollection<InstalledModInfo> Mods { get; } = new();

    public ReactiveCommand<InstalledModInfo, Unit> DeleteModCommand { get; }

    public ModListViewModel(MainViewModel main, ModpackMetadata modpack) {
        _main = main;
        // Inițializăm serviciul pentru folderul acestui modpack
        _main.modpackManifestService.OpenModpack(modpack.InstallPath);

        _ = LoadMods();

        DeleteModCommand = ReactiveCommand.CreateFromTask<InstalledModInfo>(async mod => {
            // 1. Ștergem din fișierul manifest (trebuie să ai metoda RemoveMod în serviciu)
            _main.modpackManifestService.RemoveMod(mod.ProjectId);

            // 2. Reîncărcăm lista pentru a actualiza UI-ul
            await LoadMods();
        });

        MessageBus.Current.Listen<ManifestChangedMessage>()
            .Subscribe(msg => {
                Dispatcher.UIThread.Post(() => {
                    if(msg.IsRemoved) {
                        var toRemove = Mods.FirstOrDefault(m => m.ProjectId == msg.Mod.ProjectId);
                        if(toRemove != null)
                            Mods.Remove(toRemove);
                    } else {
                        // Verificăm să nu îl avem deja în listă
                        if(Mods.All(m => m.ProjectId != msg.Mod.ProjectId)) {
                            Mods.Add(msg.Mod);
                        }
                    }
                });
            });
    }

    public async Task LoadMods() {
        Mods.Clear();
        await _main.modpackManifestService.LoadSync();
        var installed = _main.modpackManifestService.Manifest.InstalledMods;
        foreach (var mod in installed) {
            Mods.Add(mod);
        }
    }
}

public class ManifestChangedMessage {
    public required InstalledModInfo Mod { get; set; }
    public bool IsRemoved { get; set; }
}