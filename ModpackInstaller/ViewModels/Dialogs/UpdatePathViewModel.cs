using System;
using System.Reactive;
using ModpackInstaller.Models.Modrinth;
using ModpackInstaller.Services;
using ModpackInstaller.Services.Modpack;
using ReactiveUI;

namespace ModpackInstaller.ViewModels.Dialogs;

public class UpdatePathViewModel : ViewModelBase {
    public string FromVersion { get; set; } = "";
    public string ToVersion { get; set; } = "";

    public string _versionId;

    public string DisplayText => $"{FromVersion} → {ToVersion}";

    public string Changelog { get; set; } = "";

    public ReactiveCommand<Unit, Unit> UpdateToHereCommand { get; }

    public event Action<string, string>? Updated;

    public UpdatePathViewModel(string fromVersion, ModrinthVersionExtended version, ModpackManifestService manifestS) {
        FromVersion = fromVersion;
        ToVersion = version.VersionNumber;
        Changelog = version.Changelog;
        _versionId = version.Id;

        UpdateToHereCommand = ReactiveCommand.CreateFromTask(async () => {
            var version = await ModrinthApiService.GetVersionAsync(_versionId);
            if(version == null)
                return;

            var project = await ModrinthApiService.GetProjectAsync(version.ProjectId);
            if(project == null)
                return;
            await manifestS.InstallModAsync(project, version);
            Updated?.Invoke(_versionId, ToVersion);
        });
    }
}