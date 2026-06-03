using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Text;
using System.Threading.Tasks;
using ModpackInstaller.Models;
using ModpackInstaller.Models.Modrinth;
using ModpackInstaller.Services;
using ModpackInstaller.Services.Modpack;
using ReactiveUI;
using ReactiveUI.SourceGenerators;

namespace ModpackInstaller.ViewModels.Dialogs;
public partial class ModUpdateViewModel : ViewModelBase {
    [Reactive]
    private string _modName;
    [Reactive]
    private string _currentVersion;
    [Reactive]
    private string _currentVersionId;

    public ObservableCollection<UpdatePathViewModel> UpdatePaths { get; }

    public ReactiveCommand<Unit, Unit> UpdateToLatestCommand { get; }

    private readonly List<ModrinthVersionExtended> _modrinthVersions;
    private readonly ModpackManifestService manifestS;

    public ModUpdateViewModel(
        string modName, 
        string currentVersionId, 
        string currentVersion, 
        List<ModrinthVersionExtended> modrinthVersions,
        ModpackManifestService manifestS
        ) {
        ModName = modName;
        CurrentVersion = currentVersion;
        CurrentVersionId = currentVersionId;
        _modrinthVersions = modrinthVersions;
        this.manifestS = manifestS;
        UpdatePaths = [];
        string lastVersionNumber = "";
        bool started = false;
        foreach(var version in _modrinthVersions) {
            if(version.Id == CurrentVersionId) {
                lastVersionNumber = version.VersionNumber;
                started = true;
                continue;
            }
            if(!started) {
                lastVersionNumber = version.VersionNumber;
                //UpdatePaths.Insert(0, new UpdatePathViewModel("null", "null", version.Changelog));
                continue;
            }

            var path = new UpdatePathViewModel(lastVersionNumber, version, manifestS);
            path.Updated += OnPathUpdated;

            UpdatePaths.Insert(0, path);
            lastVersionNumber = version.VersionNumber;
        }

        UpdateToLatestCommand = ReactiveCommand.CreateFromTask(async () => {
            var version = await ModrinthApiService.GetVersionAsync(UpdatePaths.First()._versionId);
            if(version == null)
                return;

            var project = await ModrinthApiService.GetProjectAsync(version.ProjectId);
            if(project == null)
                return;
            await manifestS.InstallModAsync(project, version);
            OnPathUpdated(version.Id, version.VersionNumber);
        });
    }

    private void OnPathUpdated( string VersionId, string ToVersion ) {
        CurrentVersionId = VersionId;
        CurrentVersion = ToVersion;

        while(UpdatePaths.Count > 0) {
            var first = UpdatePaths.Last();

            UpdatePaths.Remove(first);

            if(first._versionId == VersionId)
                break;
        }
    }
}
