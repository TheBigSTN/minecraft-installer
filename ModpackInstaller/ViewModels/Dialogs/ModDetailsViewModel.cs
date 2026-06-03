using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Text;
using System.Threading.Tasks;
using ModpackInstaller.Models.Modrinth;
using ModpackInstaller.Models;
using ReactiveUI;
using ModpackInstaller.Services;
using ModpackInstaller.Services.Modpack;
using System.Runtime.ConstrainedExecution;
using ReactiveUI.SourceGenerators;
using Avalonia.Threading;
using ModpackInstaller.ViewModels.Sidebars;

namespace ModpackInstaller.ViewModels.Dialogs;
public partial class ModDetailsViewModel : ViewModelBase {
    [Reactive]
    public ModInfo _installedMod;
    //public string InstalledVersionDisplay =>
    //InstalledMod != null ? InstalledMod.VersionNumber : "Not Installed";

    //public bool IsModInstalled => InstalledMod != null;

    public ModrinthProject? Project { get; private set; }

	public ObservableCollection<ModVersionDisplayModel> Versions { get; }

	public ReactiveCommand<ModVersionDisplayModel, Unit> InstallVersionCommand { get; }

	//public ReactiveCommand<Unit, Unit> RefreshCommand { get; }

	public string SelectedTab { get; set; } = "";

	public ModDetailsViewModel( ModpackManifestService modpackManifestService, ModInfo modInfo, string GameVersion, string Loader ) {
		InstalledMod = modInfo;
		Versions = [];


        InstallVersionCommand = ReactiveCommand.CreateFromTask<ModVersionDisplayModel>(async ( versionExt ) => {
			var version = await ModrinthApiService.GetVersionAsync(versionExt.ModrinthVersion.Id);
			if(version == null) 
				return;

            var project = await ModrinthApiService.GetProjectAsync(version.ProjectId);
            if(project == null)
                return;

            var ModInfo = await modpackManifestService.InstallModAsync(project, version);

            if(ModInfo == null)
                return;

            Dispatcher.UIThread.Post(() => {
                MessageBus.Current.SendMessage(new ManifestChangedMessage { Mod = ModInfo });
            });

            InstalledMod = new();
            InstalledMod = ModInfo;

            bool metCurent = false;
            foreach(var v in Versions) {
                bool IsCurent = v.ModrinthVersion.VersionNumber == InstalledMod.VersionNumber;
                if(IsCurent)
                    metCurent = true;
                v.IsCurrent = IsCurent;
                v.IsUpgrade = !metCurent && !IsCurent;
                v.IsDowngrade = metCurent && !IsCurent;
            }

        });

        _ = FetchDataAsync(GameVersion,Loader);
    }

	private async Task FetchDataAsync( string GameVersion, string Loader ) {
		var project = await ModrinthApiService.GetProjectAsync(InstalledMod.ProjectId);
        List<ModrinthVersionExtended>? ver = await ModrinthApiService.GetProjectVersionAsync(InstalledMod.ProjectId, GameVersion, Loader);
		Project = project;
        this.RaisePropertyChanged(nameof(Project));
        Versions.Clear();
        if(ver == null)
            return;

        bool metCurent = false;
        foreach(var v in ver) {
            bool IsCurent = v.VersionNumber == InstalledMod.VersionNumber;
            if(IsCurent)
                metCurent = true;
            var displayModel = new ModVersionDisplayModel(v) {
                // Logica ta de comparare
                IsCurrent = IsCurent,
                IsUpgrade = !metCurent && !IsCurent,
                IsDowngrade = metCurent && !IsCurent
            };

            Versions.Add(displayModel);
        }
    }
}