using System;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using Avalonia.VisualTree;
using ModpackInstaller.ViewModels;
using ModpackInstaller.ViewModels.Body;
using ReactiveUI;

namespace ModpackInstaller.Views.Body;

public partial class ModpackInfoView : ReactiveUserControl<ModpackInfoViewModel> {
    public ModpackInfoView()
    {
        InitializeComponent();


            this.WhenActivated(disposables =>
            {
                this.WhenAnyValue(x => x.ViewModel)
                    .Where(vm => vm != null)
                    // SwitchToLatest ensures that if the VM changes, 
                    // the old interaction handler is disposed of automatically.
                    .Select(vm => vm.ShowExportDialog.RegisterHandler(async interaction => {
                        var dialogVm = new ExportModpackDialogViewModel(vm.Modpack);

                        var dialog = new ExportModpackDialogView { DataContext = dialogVm };

                        dialogVm.CloseRequested += (result) => dialog.Close(result);

                        var window = this.GetVisualRoot() as Window;
                        var result = await dialog.ShowDialog<ModpackExportMode?>(window);

                        interaction.SetOutput(result);
                    }))
                    .Subscribe()
                    .DisposeWith(disposables);
            });
        //this.WhenActivated(disposables =>
        //{
        //    // Observăm când proprietatea ViewModel se schimbă
        //    this.WhenAnyValue(x => x.ViewModel)
        //        .Where(vm => vm != null)
        //        .Subscribe(vm => {
        //            vm.ShowExportDialog.RegisterHandler(async interaction => {
        //                var dialogVm = new ExportModpackDialogViewModel();
        //                var dialog = new ExportModpackDialogView { DataContext = dialogVm };

        //                var window = this.GetVisualRoot() as Window;
        //                var result = await dialog.ShowDialog<ModpackExportMode?>(window);

        //                interaction.SetOutput(result);
        //            }).DisposeWith(disposables);
        //        })
        //        .DisposeWith(disposables);
        //});
    }
}