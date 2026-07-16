using System;
using System.Reactive;
using System.Threading.Tasks;
using ModpackInstaller.Models.Backend;
using ModpackInstaller.Models.DTOs;
using ModpackInstaller.Services;
using ModpackInstaller.Services.Modpack;
using ReactiveUI;

namespace ModpackInstaller.Models;

public class ModpackVersionItemViewModel : ReactiveObject {
    private ModpackVersionDto Version { get; set; }
    public string Semver => Version.Semver;
    public string VersionName => Version.VersionName;
    public ModpackVersionStatus Status => Version.Status;
    public DateTimeOffset CreatedAt => Version.CreatedAt;

    public ReactiveCommand<string, Unit> ChangeStatusCommand { get; }

    public ModpackVersionItemViewModel(ModpackVersionDto version) {
        Version = version;

        ChangeStatusCommand = ReactiveCommand.CreateFromTask(async (string newStatus) => {
            if (AppSettings.Settings.Config.UserPasswordToken is null) return;

            var status = Enum.Parse<ModpackVersionStatus>(newStatus);
            
            await BackendApiService.UpdateVersionStatusAsync(
                new UpdateVersionStatusRequest(
                    Guid.Parse(Version.ModpackId),
                    Version.Id, 
                    status),
                AppSettings.Settings.Config.UserPasswordToken);
            
            Version = Version with {
                Status = status
            };
            
            this.RaisePropertyChanged(nameof(Status));
        });
    }
}