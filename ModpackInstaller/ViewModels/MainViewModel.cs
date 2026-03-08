namespace ModpackInstaller.ViewModels;

using ModpackInstaller.ViewModels.Body;
using ModpackInstaller.ViewModels.Sidebars;
using ModpackInstaller.ViewModels.Topbars;
using ModpackInstaller.Models;
using ReactiveUI;
using System;
using System.IO;
using ModpackInstaller.Services.Modpack;
using ModpackInstaller.Infrastructure;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using ModpackInstaller.Services;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum InstallTarget {
	TLauncher,
	CurseForge,
	Modrinth,
	Custom
}

public class MainViewModel : ViewModelBase {

    private ViewModelBase _sidebarViewModel;
    public ViewModelBase SidebarViewModel {
        get => _sidebarViewModel;
        set => this.RaiseAndSetIfChanged(ref _sidebarViewModel, value);
    }

    private ViewModelBase _topBarViewModel;
    public ViewModelBase TopBarViewModel {
        get => _topBarViewModel;
        set => this.RaiseAndSetIfChanged(ref _topBarViewModel, value);
    }

    private ViewModelBase _bodyViewModel;
    public ViewModelBase BodyViewModel {
        get => _bodyViewModel;
        set => this.RaiseAndSetIfChanged(ref _bodyViewModel, value);
    }

    private string _searchQuery = "";
    public string SearchQuery {
        get => _searchQuery;
        set => this.RaiseAndSetIfChanged(ref _searchQuery, value);
    }

    public ModpackMetadata? SelectedModpack { get; private set; }

	public AppSettings Settings { get; } = new AppSettings(AppVariables.InstallerRoot);

    public MainViewModel() {
		InstallTarget = Settings.Config.InstallTarget;

		ShowGlobal();
    }

    public void ShowGlobal(ModpackMetadata? modpack) {
        SidebarViewModel = new ModpackListViewModel(this);
        TopBarViewModel = new GlobalTopBarViewModel(this);
        BodyViewModel = new ModpackInfoViewModel(modpack, this);
        _sidebarViewModel = SidebarViewModel;
        _topBarViewModel = TopBarViewModel;
        _bodyViewModel = BodyViewModel;
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
        _sidebarViewModel = SidebarViewModel;
        _topBarViewModel = TopBarViewModel;
        _bodyViewModel = BodyViewModel;
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

        SidebarViewModel = new ModListViewModel(modpack);
        TopBarViewModel = new ModpackTopBarViewModel(this);
        BodyViewModel = new ModrinthBrowserViewModel(modpack, this);
    }

    public void ShowDiscovery() {
        SidebarViewModel = new ModpackListViewModel(this);
        TopBarViewModel = new GlobalTopBarViewModel(this);
        BodyViewModel = new ModpackDiscoveryViewModel(this, new DialogService());
    }

    public void RefreshModpackList() {
        SidebarViewModel = new ModpackListViewModel(this);
    }

    private InstallTarget _installTarget;

	public InstallTarget InstallTarget {
		get => _installTarget;
		set {
            this.RaiseAndSetIfChanged(ref _installTarget, value);
            Settings.Update(s => s.InstallTarget = InstallTarget);
        }
	}
	
    public string InstallPath {
        get {
            string basePath = InstallTarget switch {
                InstallTarget.TLauncher => Environment.OSVersion.Platform switch {
                    PlatformID.Win32NT => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), ".minecraft", "versions"),
                    PlatformID.Unix => Path.Combine(Environment.GetEnvironmentVariable("HOME")!, ".minecraft", "versions"),
                    PlatformID.MacOSX => Path.Combine(Environment.GetEnvironmentVariable("HOME")!, ".minecraft", "versions"),
                    _ => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)!, ".minecraft", "versions")
                },
                InstallTarget.CurseForge => Environment.OSVersion.Platform switch {
                    PlatformID.Win32NT => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "curseforge", "minecraft", "Instances"),
                    PlatformID.Unix => Path.Combine(Environment.GetEnvironmentVariable("HOME")!, ".curseforge", "minecraft", "Instances"),
                    PlatformID.MacOSX => Path.Combine(Environment.GetEnvironmentVariable("HOME")!, "Library", "Application Support", "minecraft", "Instances"),
                    _ => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)!, "curseforge", "minecraft", "Instances")
                },
                InstallTarget.Modrinth => Environment.OSVersion.Platform switch {
                    PlatformID.Win32NT => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ModrinthApp", "profiles"),
                    PlatformID.Unix => Path.Combine(Environment.GetEnvironmentVariable("HOME")!, ".modrinth"),
                    PlatformID.MacOSX => Path.Combine(Environment.GetEnvironmentVariable("HOME")!, "Library", "Application Support", "Modrinth"),
                    _ => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)!, "ModrinthApp")
                },
                _ => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)!, "Minecraft")
            };
            
            return basePath;
        }
    }
}
