using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ModpackInstaller.Services;
using ModpackInstaller.Models;
using Microsoft.Win32;
using ModpackInstaller.Infrastructure;
using ModpackInstaller.Services.Modpack;

namespace ModpackInstaller.ViewModels.Sidebars;
public class ModpackListViewModel : ViewModelBase {
    private readonly MainViewModel _main;
    private readonly ModpackMedatataService _registry;

    public ObservableCollection<ModpackMetadata> Modpacks { get; } = new();

    private ModpackMetadata? _selected;
    public ModpackMetadata? SelectedModpack {
        get => _selected;
        set {
            _selected = value;
            if (value != null)
                _main.OpenModpack(value);
        }
    }

    public ModpackListViewModel(MainViewModel main) {
        _main = main;
        _registry = new ModpackMedatataService(AppVariables.InstallerRoot);

        LoadModpacks();
        // load installed modpacks
        //ModpackMedatataService.Add(new ModpackMetadata { Name = "Vanilla+" });
        //ModpackMedatataService.Add(new ModpackMetadata { Name = "Create Pack" });
    }

    public void LoadModpacks() {
        Modpacks.Clear();

        foreach (var modpack in _registry.LoadAll()) {
            Modpacks.Add(modpack);
        }
    }
}
