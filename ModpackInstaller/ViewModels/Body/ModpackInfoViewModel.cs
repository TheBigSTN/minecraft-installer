using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using ModpackInstaller.Infrastructure;
using ModpackInstaller.Models;
using ModpackInstaller.Services;
using ModpackInstaller.Services.Modpack;
using ReactiveUI;

namespace ModpackInstaller.ViewModels.Body;
public enum ModpackExportMode {
	LocalZip,
	Unlisted,
	Public
}


public class ModpackInfoViewModel : ViewModelBase {

	public ModpackMetadata? Modpack { get; set; }

	private readonly MainViewModel _main;
	private readonly ModpackMedatataService _medatataService = new();
	private readonly ModpackManifestService _manifestService;

	//public Interaction<Unit, ModpackExportMode?> ShowExportDialog { get; }
	//= new();

	public ReactiveCommand<Unit, Unit> PBUpdateModpackCommand { get; }
	public ReactiveCommand<Unit, Unit> EditModpackCommand { get; }
	public ReactiveCommand<Unit, Unit> ExportModpackCommand { get; }
    public ReactiveCommand<Unit, Unit> DeleteModpackCommand { get; }
    public ReactiveCommand<Unit, Unit> OpenModUpdatesWindowCommand { get; }

    private bool _hasUpdate;
	public bool HasUpdate {
		get => _hasUpdate;
		set => this.RaiseAndSetIfChanged(ref _hasUpdate, value);
	}

	public ReactiveCommand<Unit, Unit> UpdateModpackCommand { get; }

	public ModpackInfoViewModel(ModpackMetadata? modpack, MainViewModel main) {
		Modpack = modpack;
		_main = main;

		var canUpdate = this.WhenAnyValue(
			x => x.Modpack,
			(ModpackMetadata? mp) => mp != null && !string.IsNullOrEmpty(mp.ModpackPassword)
		);

		_ = CheckForUpdateAsync();
		_manifestService = ModpackManifestService.CreateInstance(Modpack?.InstallPath ?? "");
		_ = _manifestService.SyncWithFilesystemAsync();
		if (modpack != null)
			_ = _manifestService.SyncToFileSistemAsync(modpack.IsServerInstall);
		
   //      OpenModUpdatesWindowCommand = ReactiveCommand.CreateFromTask(async () => {
			// if(Modpack == null)
   //              return;
			// await _main.DialogService.ShowModsUpdateDialog(Modpack.Id);
			// _main.RefreshModpackList();
   //      });


    }

	private async Task CheckForUpdateAsync() {
		if (Modpack == null)
			return;

		try {
			// var serverInfo = await BackendApiService.GetModpack(Modpack.Id, Modpack.SharingCode);
			//
			// if (serverInfo == null)
				return;

			// HasUpdate = serverInfo.LatestVersion > Modpack.Version;
		} catch (Exception e) {
			_ = e;
			HasUpdate = false;
		}
	}

	public bool HasModpack => Modpack != null;
}