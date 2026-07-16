using System.Reactive;
using ReactiveUI;

namespace ModpackInstaller.ViewModels.Dialogs;

public class ModpackConflictDialogViewModel : DialogViewModel<bool> {
    public string InstanceName { get; }

    public ReactiveCommand<Unit, Unit> CancelCommand { get; }
    public ReactiveCommand<Unit, Unit> DuplicateCommand { get; }

    public ModpackConflictDialogViewModel(string instanceName)
    {
        InstanceName = instanceName;

        CancelCommand = ReactiveCommand.Create(() =>
            Close(false));
        
        DuplicateCommand = ReactiveCommand.Create(() =>
            Close(true));
            
        // OpenInstanceCommand = ReactiveCommand.Create(() =>
        //     // Close(ModpackConflictResult.OpenInstance));
        //     Close(Unit.Default));
        //
        // CreateCommand = ReactiveCommand.Create(() =>
        //     // Close(ModpackConflictResult.CreateDuplicate));
        //     Close(Unit.Default));
    }
}

public enum ModpackConflictResult
{
    Cancel,
    OpenInstance,
    CreateDuplicate
}