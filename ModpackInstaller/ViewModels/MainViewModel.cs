namespace ModpackInstaller.ViewModels;

using ModpackInstaller.ViewModels.Body;
using ModpackInstaller.ViewModels.Sidebars;
using ModpackInstaller.ViewModels.Topbars;
using ModpackInstaller.Models;
using ReactiveUI;
using ModpackInstaller.Services.Modpack;
using ModpackInstaller.Infrastructure;
using System.Diagnostics.CodeAnalysis;
using ModpackInstaller.Services;
using ReactiveUI.SourceGenerators;

public partial class MainViewModel : ViewModelBase {
    public IDialogService DialogService;

    [Reactive]
    private ViewModelBase _sidebarViewModel;

    [Reactive]
    private ViewModelBase _topBarViewModel;

    [Reactive]
    private ViewModelBase _bodyViewModel;

    [Reactive]
    private string _searchQuery = "";


    public ModpackManifestService modpackManifestService;


    public ModpackMetadata? SelectedModpack { get; private set; }

	public AppSettings Settings { get; } = new AppSettings(AppVariables.InstallerRoot);

    public MainViewModel(IDialogService dialogService) {
		InstallTarget = Settings.Config.InstallTarget;
        modpackManifestService = new ModpackManifestService();

        DialogService = dialogService;
        
        ShowGlobal();
    }

    public void ShowGlobal(ModpackMetadata? modpack) {
        SidebarViewModel = new ModpackListViewModel(this);
        TopBarViewModel = new GlobalTopBarViewModel(this);
        BodyViewModel = new ModpackInfoViewModel(modpack, this);
        //_sidebarViewModel = SidebarViewModel;
        //_topBarViewModel = TopBarViewModel;
        //_bodyViewModel = BodyViewModel;
    }

    [MemberNotNull(
    nameof(_sidebarViewModel),
    nameof(_topBarViewModel),
    nameof(_bodyViewModel)
    )]
    public void ShowGlobal() {
        SidebarViewModel = new ModpackListViewModel(this);
        TopBarViewModel = new GlobalTopBarViewModel(this);
        BodyViewModel = new ModpackInfoViewModel(null, this);
        //_sidebarViewModel = SidebarViewModel;
        //_topBarViewModel = TopBarViewModel;
        //_bodyViewModel = BodyViewModel;
    }

    public void OpenModpack(ModpackMetadata modpack) {
        SelectedModpack = modpack;
        if (SidebarViewModel is not ModpackListViewModel)
            SidebarViewModel = new ModpackListViewModel(this);
        if (SidebarViewModel is not GlobalTopBarViewModel)
            TopBarViewModel = new GlobalTopBarViewModel(this);
        BodyViewModel = new ModpackInfoViewModel(modpack, this);
    }

    public void EditModpack(ModpackMetadata modpack) {
        SelectedModpack = modpack;

        SidebarViewModel = new ModListViewModel(this, modpack);
        TopBarViewModel = new ModpackTopBarViewModel(this);
        BodyViewModel = new ModrinthBrowserViewModel(modpack, this);
    }

    public void ShowDiscovery() {
        SidebarViewModel = new ModpackListViewModel(this);
        TopBarViewModel = new GlobalTopBarViewModel(this);
        BodyViewModel = new ModpackDiscoveryViewModel(this);
    }

    public void RefreshModpackList() {
        if (SidebarViewModel is ModpackListViewModel modpackList) {
            modpackList.LoadModpacks();
        }
    }

    private InstallPlatform _installTarget;

	public InstallPlatform InstallTarget {
		get => _installTarget;
		set {
            this.RaiseAndSetIfChanged(ref _installTarget, value);
            Settings.Update(s => s.InstallTarget = InstallTarget);
        }
	}
	
    public string InstallPath {
        get => AppVariables.GetBaseInstallPathFromLauncer(InstallTarget);
    }
}
