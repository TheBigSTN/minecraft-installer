using System.IO;
using ModpackInstaller.Infrastructure;
using ModpackInstaller.Models;

namespace ModpackInstaller.ViewModels.Dialogs;

public class ConfirmStepViewModel( CreateModpackFlowViewModel parent ) : StepViewModelBase(parent) {
    public string Name => Parent.Name;

    public string GameVersion => Parent.SelectedGameVersion;

    public ModLoaderType Loader => Parent.SelectedLoader;

    public string LoaderVersion => Parent.SelectedLoaderVersion;

    public string InstallPath =>
        Path.Combine(AppVariables.GetBaseInstallPathFromLauncer(Parent.SelectedModPlatform), Name);
    public string Message =>
        $"Because of development reasons before creating the modpack '{Name}'?\n\n" +
        $"You need to create a modpack in the app {Parent.SelectedModPlatform} with:\n" +
        $"- Name: {Name}\n" +
        $"- Minecraft version: {GameVersion}\n" +
        $"- Loader: {Loader}\n" +
        $"- Loader version: {LoaderVersion}\n" +
        $"Click finish after you create the modpack in the specified app";
}