using System.Reactive;
using ModpackInstaller.Models;
using ModpackInstaller.Models.DTOs;
using ModpackInstaller.Services.Modpack;
using ReactiveUI;

namespace ModpackInstaller.ViewModels.Dialogs;

public class ManualSetupDialogViewModel : DialogViewModel<ModpackManualSetupResponse> {
    private readonly IManualSetupPropData _metadata;
    public ReactiveCommand<Unit, Unit> DoneCommand { get; }
    public ReactiveCommand<Unit, Unit> CloseCommand { get; }

    public ManualSetupDialogViewModel(IManualSetupPropData metadata) {
        _metadata = metadata;

        DoneCommand = ReactiveCommand.Create(() => Close(ModpackManualSetupResponse.Finished));
        CloseCommand = ReactiveCommand.Create(() => Close(ModpackManualSetupResponse.Canceled));
    }

    public string ModpackLoader => AppSettings.Settings.Config.InstallTarget.ToString();
    public string Name => _metadata.ModpackName;
    public string GameVersion => _metadata.GameVersion;
    public ModLoaderType Loader => _metadata.Loader;
    public string LoaderVersion => _metadata.LoaderVersion;
}

public interface IManualSetupPropData{
    string ModpackName { get; }
    string GameVersion { get; }
    ModLoaderType Loader { get; }
    string LoaderVersion { get; }
}

public class ManualSetupPropData : IManualSetupPropData {
    public required string ModpackName { get; init; }
    public required string GameVersion { get; init;}
    public required ModLoaderType Loader { get; init;}
    public required string LoaderVersion { get; init;}
}

public enum ModpackManualSetupResponse {
    Finished,
    Canceled,
    Failed
}