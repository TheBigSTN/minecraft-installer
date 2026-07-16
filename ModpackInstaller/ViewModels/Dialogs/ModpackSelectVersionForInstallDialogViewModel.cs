using System;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reactive;
using System.Threading.Tasks;
using DynamicData;
using DynamicData.Kernel;
using ModpackInstaller.Models.Backend;
using ModpackInstaller.Models.DTOs;
using ModpackInstaller.Services;
using ReactiveUI;

namespace ModpackInstaller.ViewModels.Dialogs;

public class ModpackSelectVersionForInstallDialogViewModel : DialogViewModel<ModpackVersionDto?> {
    private readonly Guid _modpackId;

    public ObservableCollection<ModpackVersionDto> ModpackVersions { get; }
    
    public ReactiveCommand<ModpackVersionDto, Unit> SelectVersionCommand { get; }
    public ReactiveCommand<Unit, Unit> CloseCommand { get; }

    private ModpackSelectVersionForInstallDialogViewModel(Guid modpackId) {
        ModpackVersions = [];
        _modpackId = modpackId;
        
        SelectVersionCommand = ReactiveCommand.Create((ModpackVersionDto version) => {
            Close(version);
        });
        
        CloseCommand = ReactiveCommand.Create(() => {
            Close(null);
        });
    }

    public static async Task<ModpackSelectVersionForInstallDialogViewModel> CreateInstance(Guid modpackId) {
        var vm = new ModpackSelectVersionForInstallDialogViewModel(modpackId);

        await vm.LoadAsync();
        
        return vm;
    }

    private async Task LoadAsync() {
        var versions = await BackendApiService.GetModpackVersionsAsync(_modpackId);
        var filtered = versions.Where(v => v.Status == ModpackVersionStatus.Release);
        
        ModpackVersions.Clear();
        ModpackVersions.AddRange(filtered);
    }
}