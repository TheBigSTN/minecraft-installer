using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using ModpackInstaller.Infrastructure;
using ModpackInstaller.Models;
using ModpackInstaller.Services.Modpack;
using ModpackInstaller.ViewModels.Body;
using ReactiveUI;

namespace ModpackInstaller.ViewModels;

public class ExportModpackDialogViewModel : ViewModelBase {
    private readonly ModpackMetadata _localModpack;

    public ReactiveCommand<Unit, ModpackExportMode> ExportZipCommand { get; }
    public ReactiveCommand<Unit, ModpackExportMode> ExportPublicCommand { get; }
    public ReactiveCommand<Unit, ModpackExportMode> ExportUnlistedCommand { get; }
    public ReactiveCommand<Unit, Unit> CancelCommand { get; }

    public event Action<ModpackExportMode?>? CloseRequested;

    public ExportModpackDialogViewModel(ModpackMetadata modpack) {
        _localModpack = modpack;

        var canPublish = Observable.Return(string.IsNullOrEmpty(_localModpack.ModpackPassword));

        ExportZipCommand = ReactiveCommand.Create(() =>
            Close(ModpackExportMode.LocalZip));

        ExportPublicCommand = ReactiveCommand.Create(
                    () => Close(ModpackExportMode.Public),
                    canPublish);

        ExportUnlistedCommand = ReactiveCommand.Create(
            () => Close(ModpackExportMode.Unlisted),
            canPublish);

        CancelCommand = ReactiveCommand.Create(() =>
            Close(null));
    }

    //private bool ValidateForUpload() {
    //    // 1. Verificăm dacă userul este înregistrat (are UserToken)
    //    if (string.IsNullOrEmpty(_settings.Config.UserPasswordToken)) {
    //        // Aici ar fi bine să declanșezi un mesaj în UI: "Trebuie să te înregistrezi în setări!"
    //        return false;
    //    }

    //    // 2. Verificăm dacă modpack-ul este deja "publicat" (are ID)
    //    // Dacă are ID, înseamnă că butonul de "Make Public" nu mai e valid, 
    //    // trebuie folosit butonul de "Update" (pe care ai zis că îl vei avea separat)
    //    if (!string.IsNullOrEmpty(_localModpack.ModpackPassword)) {
    //        // "Acest modpack este deja pe server. Folosește butonul de Update."
    //        return false;
    //    }

    //    return true;
    //}

    private ModpackExportMode Close(ModpackExportMode mode) {
        CloseRequested?.Invoke(mode);
        return mode;
    }

    private void Close(ModpackExportMode? mode) {
        CloseRequested?.Invoke(mode);
    }
}