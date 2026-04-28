using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Controls;
using ModpackInstaller.Infrastructure;
using ModpackInstaller.Models;
using ModpackInstaller.Services.Modpack;
using ModpackInstaller.Views;
using ReactiveUI;

namespace ModpackInstaller.ViewModels.Topbars;

public class GlobalTopBarViewModel : ViewModelBase {

    public IEnumerable<InstallPlatform> InstallTargets { get; } =
        Enum.GetValues<InstallPlatform>();

    private InstallPlatform _selectedInstallTarget;

    public InstallPlatform SelectedInstallTarget {
        get => _selectedInstallTarget;
        set {
            this.RaiseAndSetIfChanged(ref _selectedInstallTarget, value);
            _main.InstallTarget = value; // sincronizează și în settings
        }
    }

    public ReactiveCommand<Unit, Unit> CreateModpack { get; }
	public ReactiveCommand<Unit, Unit> OpenSettings { get; }
    public ReactiveCommand<Unit, Unit> OpenDiscovery { get; }

    private readonly MainViewModel _main;
	public GlobalTopBarViewModel(MainViewModel main) {

		_main = main;
        _selectedInstallTarget = _main.InstallTarget;

        CreateModpack = ReactiveCommand.CreateFromTask(async () =>
        {
            var metadata = await _main.DialogService.ShowCreateModpackDialog(_main.InstallPath, _main.InstallTarget.ToString());

            if (metadata == null)
                return;

            var registry = new ModpackMedatataService(AppVariables.InstallerRoot);
            registry.Create(metadata);

            _main.OpenModpack(metadata);

            _main.RefreshModpackList();
        });

        OpenDiscovery = ReactiveCommand.Create(() =>
        {
            _main.ShowDiscovery();
        });

        OpenSettings = ReactiveCommand.Create(() => {
            // logica deschidere settings
            Console.WriteLine("Settings clicked");
        });
    }
}
