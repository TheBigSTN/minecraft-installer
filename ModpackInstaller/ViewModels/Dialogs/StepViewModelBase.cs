using System;
using System.Linq;
using System.Text;
using ReactiveUI;

namespace ModpackInstaller.ViewModels.Dialogs;

public abstract class StepViewModelBase( CreateModpackFlowViewModel parent ) : ReactiveObject {
    protected CreateModpackFlowViewModel Parent { get; } = parent;
}
