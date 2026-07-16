
using System.Collections.Generic;

namespace ModpackInstaller.ViewModels;

using Body;
using Sidebars;
using Models;
using System.Threading.Tasks;
using System.Diagnostics.CodeAnalysis;
using ReactiveUI.SourceGenerators;

public partial class MainViewModel : ViewModelBase {

    [Reactive]
    private SideNavigationBarViewModel _sideNavigationViewModel;

    [Reactive]
    private ViewModelBase _bodyViewModel;
    
    public MainViewModel() {
        SideNavigationViewModel = new SideNavigationBarViewModel(this);
        OpenHome();
    }

    [MemberNotNull(nameof(_bodyViewModel))]
    public void OpenHome() {
        BodyViewModel = new HomePageViewModel();
    }

    public void OpenDiscovery() {
        BodyViewModel = new DiscoveryPageViewModel(this);
    }
    
    public void OpenDiscovery(ModpackMetadata modpack) {
        BodyViewModel = new DiscoveryPageViewModel(this, modpack);
    }
    
    public void OpenModpack(ModpackMetadata modpack) =>
        BodyViewModel = new ModpackPageViewModel(this, modpack);
    
    [Reactive] 
    private ViewModelBase? _currentDialog;

    [Reactive]
    private bool _isDialogOpen;

    [Reactive] 
    private int _dialogBlurRadius; 
    
    private readonly Stack<ViewModelBase> _dialogStack = new();
    
    public async Task<T> ShowDialog<T>(DialogViewModel<T> dialog) {
        _dialogStack.Push(dialog);

        CurrentDialog = dialog;
        IsDialogOpen = true;
        DialogBlurRadius = 8;

        var result = await dialog.WaitAsync();

        _dialogStack.Pop();

        if (_dialogStack.TryPeek(out var previous)) {
            CurrentDialog = previous;
        } else {
            CurrentDialog = null;
            IsDialogOpen = false;
            DialogBlurRadius = 0;
        }

        return result;
    }
    
    
}
