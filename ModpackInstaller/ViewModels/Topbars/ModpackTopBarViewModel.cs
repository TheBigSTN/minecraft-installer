using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Text;
using System.Threading.Tasks;
using ModpackInstaller.ViewModels.Sidebars;
using ModpackInstaller.Views;
using ReactiveUI;

namespace ModpackInstaller.ViewModels.Topbars;

public class ModpackTopBarViewModel : ViewModelBase {
    private readonly MainViewModel _main;
    public string SearchQuery {
        get => _main.SearchQuery;
        set => _main.SearchQuery = value;
    }

    public ReactiveCommand<Unit, Unit> GoBackCommand { get; }

    public ModpackTopBarViewModel(MainViewModel main) {
        _main = main;

        GoBackCommand = ReactiveCommand.Create(() => {
            // Aici setezi body-ul la lista de modpack-uri
            _main.ShowGlobal(_main.SelectedModpack);
        });
    }
}