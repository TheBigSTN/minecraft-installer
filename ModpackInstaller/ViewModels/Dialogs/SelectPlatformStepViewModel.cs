using ModpackInstaller.Models;
using ReactiveUI;
using System.Collections.Generic;
using System.Reactive;
using System;

namespace ModpackInstaller.ViewModels.Dialogs;

public class SelectPlatformStepViewModel : StepViewModelBase {
    public IEnumerable<InstallPlatform> Platforms => Enum.GetValues<InstallPlatform>();

    public ReactiveCommand<InstallPlatform, Unit> SelectPlatformCommand { get; }

    public SelectPlatformStepViewModel( CreateModpackFlowViewModel parent ) : base(parent) {
        SelectPlatformCommand = ReactiveCommand.Create<InstallPlatform>(platform => {
            Parent.SelectedModPlatform = platform;

            Parent.Next.Execute().Subscribe();
        });
    }
}