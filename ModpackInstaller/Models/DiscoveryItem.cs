using System.Reactive;
using ReactiveUI;
using ReactiveUI.SourceGenerators;

namespace ModpackInstaller.Models;

public class DiscoveryItem : ReactiveObject {
    private readonly string _id = null!;
    public required string Id {
        get => _id;
        init => this.RaiseAndSetIfChanged(ref _id, value);
    }

    private string? _iconUrl;
    public string? IconUrl {
        get => _iconUrl;
        set => this.RaiseAndSetIfChanged(ref _iconUrl, value);
    }

    private string _title = null!;
    public required string Title {
        get => _title;
        set => this.RaiseAndSetIfChanged(ref _title, value);
    }

    private string _authorName = null!;
    public required string AuthorName {
        get => _authorName;
        set => this.RaiseAndSetIfChanged(ref _authorName, value);
    }

    private string _description = null!;
    public required string Description {
        get => _description;
        set => this.RaiseAndSetIfChanged(ref _description, value);
    }

    private bool _isInstalled;
    public required bool IsInstalled {
        get => _isInstalled;
        set {
            this.RaiseAndSetIfChanged(ref _isInstalled, value);
            this.RaisePropertyChanged(nameof(InstallText));
        }
    }
    
    public string InstallText =>
        IsInstalling
            ? "Installing..."
            : IsInstalled
                ? "Installed"
                : "+ Install";
    
    private bool _isInstalling;
    public required bool IsInstalling {
        get => _isInstalling;
        set {
            this.RaiseAndSetIfChanged(ref _isInstalling, value);
            this.RaisePropertyChanged(nameof(InstallText));
        }
    }

    private object? _source;
    public object? Source {
        get => _source;
        set => this.RaiseAndSetIfChanged(ref _source, value);
    }
    
    public required ReactiveCommand<DiscoveryItem, Unit> InstallCommand { get; init; }
}