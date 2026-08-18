
using System.Collections.Generic;
using System.Collections.ObjectModel;
using DynamicData;
using ModpackInstaller.Services.Modpack;
using ModpackInstaller.Services.Notifications;

namespace ModpackInstaller.ViewModels;

using Body;
using Sidebars;
using System.Threading.Tasks;
using System.Diagnostics.CodeAnalysis;
using ReactiveUI.SourceGenerators;

public partial class MainViewModel : ViewModelBase {
    // public static MainViewModel Instance { get; private set; } = null!;

    [Reactive]
    private SideNavigationBarViewModel _sideNavigationViewModel;

    [Reactive]
    private ViewModelBase _bodyViewModel;
    
    public MainViewModel() {
        NotificationManager.Notifications.Changed += list => {
            NotificationStack.Clear();
            NotificationStack.AddRange(list);
        };
        NotificationManager.IsConsole = false;
        SideNavigationViewModel = new SideNavigationBarViewModel(this);
        OpenHome();

        // Instance = this;
    }

    [MemberNotNull(nameof(_bodyViewModel))]
    public void OpenHome() {
        BodyViewModel = new HomePageViewModel();
    }

    public void OpenDiscovery() {
        BodyViewModel = new DiscoveryPageViewModel(this);
    }
    
    public async Task OpenDiscoveryAsync(ModpackMetadataStorage modpack) {
        BodyViewModel = await DiscoveryPageViewModel.CreateInstanceAsync(this, modpack).ConfigureAwait(false);
    }
    
    public async Task OpenModpackAsync(ModpackMetadataStorage modpackStorage) =>
        BodyViewModel = await ModpackPageViewModel.CreateInstanceAsync(this, modpackStorage)
                                                  .ConfigureAwait(true);
    
    [Reactive] 
    private ViewModelBase? _currentDialog;

    [Reactive]
    private bool _isDialogOpen;

    [Reactive] 
    private int _dialogBlurRadius; 
    
    private readonly Stack<ViewModelBase> _dialogStack = new();
    
    public async Task<T> ShowDialogAsync<T>(DialogViewModel<T> dialog) {
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

    public ObservableCollection<NotificationCardViewModel> NotificationStack = [];


}
