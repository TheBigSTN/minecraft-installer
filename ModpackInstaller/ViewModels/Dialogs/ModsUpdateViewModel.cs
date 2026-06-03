using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Text;
using System.Threading.Tasks;
using Avalonia.OpenGL;
using ModpackInstaller.Models;
using ModpackInstaller.Services;
using ModpackInstaller.Services.Modpack;
using Splat.ModeDetection;

namespace ModpackInstaller.ViewModels.Dialogs;
internal class ModsUpdateViewModel {
	private readonly List<ModInfo> _installedMods;
	private readonly ModpackMetadata? _modpackMetadata;

	private readonly ModpackManifestService _manifestService;
	private readonly ModpackMedatataService _metadataService = new();

	public ObservableCollection<ModUpdateViewModel> ModUpdates { get; } = [];

	public event Action<Unit>? CloseRequested;

	internal ModsUpdateViewModel( string modpackId ) {
		_modpackMetadata = _metadataService.Load(modpackId);

		if(_modpackMetadata == null)
			throw new Exception("Modpack metadata not found");

		_manifestService = new(_modpackMetadata.InstallPath);
		_installedMods = _manifestService.Manifest.InstalledMods;

		foreach(var mod in _installedMods) {
			_ = LoadModUpdates(mod);
		}
	}

	public async Task LoadModUpdates(ModInfo mod) {
		if(_modpackMetadata == null)
			throw new Exception("Modpack metadata not found");

		var modVersions = await ModrinthApiService.GetProjectVersionAsync(mod.ProjectId, _modpackMetadata.GameVersion, _modpackMetadata.Loader.ToString().ToLower()) ?? [];
		modVersions.Reverse();

		if(mod.VersionNumber == "") {
			var version = await ModrinthApiService.GetVersionAsync(mod.VersionId);
			if(version != null)
				mod.VersionNumber = version.VersionNumber;
		}

		if(modVersions.Last().Id != mod.VersionId && mod.Source == ModSource.Remote)
			ModUpdates.Add(new ModUpdateViewModel( mod.Title, mod.VersionId, mod.VersionNumber, modVersions, _manifestService));
	}
}
