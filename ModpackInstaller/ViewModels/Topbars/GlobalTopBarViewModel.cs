using System;
using System.Collections.Generic;
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

    public IEnumerable<InstallTarget> InstallTargets { get; } =
        Enum.GetValues<InstallTarget>();

    private InstallTarget _selectedInstallTarget;

    public InstallTarget SelectedInstallTarget {
        get => _selectedInstallTarget;
        set {
            this.RaiseAndSetIfChanged(ref _selectedInstallTarget, value);
            _main.InstallTarget = value; // sincronizează și în settings
        }
    }

    public ReactiveCommand<Unit, Unit> CreateModpack { get; }
	public ReactiveCommand<Unit, Unit> OpenSettings { get; }
    public ReactiveCommand<Unit, Unit> OpenDiscovery { get; }
    public Func<Task<ModpackMetadata?>>? ShowCreateModpackDialog { get; set; }

    private readonly MainViewModel _main;
	public GlobalTopBarViewModel(MainViewModel main) {

		_main = main;
        _selectedInstallTarget = _main.InstallTarget;

        CreateModpack = ReactiveCommand.CreateFromTask(async () =>
        {
            if (ShowCreateModpackDialog == null)
                return;

            var metadata = await ShowCreateModpackDialog();

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

        //OpenSettings = ReactiveCommand.Create(() =>
        //{
        //	// logica deschidere settings
        //	Console.WriteLine("Settings clicked");
        //});
    }
}
