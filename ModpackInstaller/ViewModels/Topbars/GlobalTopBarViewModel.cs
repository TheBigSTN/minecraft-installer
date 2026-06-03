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
using ModpackInstaller.Models.Modrinth;
using ModpackInstaller.Services;
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

        //OpenSettings = ReactiveCommand.CreateFromTask(async () => {
        //    var mod = new ModInfo {
        //        ProjectId = "aC3cM3Vq",
        //        VersionId = "aVJiOMeh",
        //        VersionNumber = "26.1-2.31-forge",
        //        Source = ModSource.Remote,
        //        Title = "Mouse Tweaks",
        //        Filename = "MouseTweaks-forge-mc26.1-2.31.jar",
        //        DownloadUrl = "https://cdn.modrinth.com/data/aC3cM3Vq/versions/aVJiOMeh/MouseTweaks-forge-mc26.1-2.31.jar",
        //        IconUrl = "https://cdn.modrinth.com/data/aC3cM3Vq/6c0eaa4e60a9c87f4766f222ff63286f09da32c0_96.webp",
        //        Enabled = true,
        //        ClientSide = SideSupport.required,
        //        ServerSide = SideSupport.unsupported
        //    };

        //    ModpackManifestService modpackManifestService = new("C:\\Users\\SARA\\curseforge\\minecraft\\Instances\\Test\\just for testing");

        //    await _main.DialogService.ShowModDetailsDialog(modpackManifestService, mod);
        //});

        //OpenSettings = ReactiveCommand.Create(() => {
        //    // Debug stuff
        //    //_main.DialogService.ShowModsUpdateDialog("274653ea5fab41f498fd79b42758ca1a");
        //    // logica deschidere settings
        //    Console.WriteLine("Settings clicked");
        //});
    }
}
