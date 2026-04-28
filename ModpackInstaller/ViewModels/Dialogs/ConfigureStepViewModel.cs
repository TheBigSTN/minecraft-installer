using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using ModpackInstaller.Models;
using ModpackInstaller.Services;
using ReactiveUI;

namespace ModpackInstaller.ViewModels.Dialogs;

public class ConfigureStepViewModel : StepViewModelBase {
	private readonly ModrinthApiService _apiService = new();

	private string _name;

	public string Name {
		get => _name;
		set {
			this.RaiseAndSetIfChanged(ref _name, value);
			Parent.Name = value;
		}
	}

	public IEnumerable<ModLoaderType> Loaders => Enum.GetValues<ModLoaderType>();

	private ModLoaderType _selectedLoader;
	public ModLoaderType SelectedLoader {
		get => _selectedLoader;
		set {
			this.RaiseAndSetIfChanged(ref _selectedLoader, value);
			Parent.SelectedLoader = value;
            _ = LoadGameVersions();
        }
	}

	private IEnumerable<string> _gameVersions;
	public IEnumerable<string> GameVersions {
		get => _gameVersions;
		private set => this.RaiseAndSetIfChanged(ref _gameVersions, value);
	}

	private string _selectedGameVersion;

	public string SelectedGameVersion {
		get => _selectedGameVersion;
		set {
			this.RaiseAndSetIfChanged(ref _selectedGameVersion, value);
			Parent.SelectedGameVersion = value;
			_ = UpdateLoaderVersions();
		}
	}

	private IEnumerable<string> _loaderVersions;

	public IEnumerable<string> LoaderVersions {
		get => _loaderVersions;
		private set => this.RaiseAndSetIfChanged(ref _loaderVersions, value);
	}

	private string _selectedLoaderVersion;
	public string SelectedLoaderVersion {
		get => _selectedLoaderVersion;
		set {
			this.RaiseAndSetIfChanged(ref _selectedLoaderVersion, value);
			Parent.SelectedLoaderVersion = value;
		}
	}

	public ConfigureStepViewModel( CreateModpackFlowViewModel parent ) : base(parent) {
		_name = parent.Name;
        _selectedLoader = parent.SelectedLoader; // ModLoaderType.NeoForge; //

        _selectedGameVersion = parent.SelectedGameVersion; //"test"; // 
        _selectedLoaderVersion = parent.SelectedLoaderVersion; //"test"; // 

        if(!string.IsNullOrWhiteSpace(_selectedGameVersion))
            _gameVersions = [_selectedGameVersion];
        else
            _gameVersions = [];
        if (!string.IsNullOrWhiteSpace(_selectedLoaderVersion))
            _loaderVersions = [ _selectedLoaderVersion ];
        else
            _loaderVersions = [];

        _ = LoadGameVersions(_selectedGameVersion, _selectedLoaderVersion);
    }

	// ---------------- ACTIONS ----------------

	public async Task SetLoaderAsync( ModLoaderType loader ) {
		if (_selectedLoader != loader)
			await LoadGameVersions();	
		SelectedLoader = loader;
	}
	public async Task SetLoaderVersionAsync( string version ) {
        if (_selectedLoaderVersion != version)
			await UpdateLoaderVersions();
        SelectedLoaderVersion = version;
	}

    private async Task LoadGameVersions(string? defaultGameVersion = null, string? defaultLoaderVersion = null) {
        var versions = await _apiService.GetGameVersionsAsync(SelectedLoader);

        GameVersions = versions
            .Where(v =>
                !string.IsNullOrWhiteSpace(v) &&
                !v.StartsWith("$") &&
                !v.Contains("{") &&
                !v.Contains("}"))
            .OrderByDescending(v => v, Comparer<string>.Create(CompareMcVersions))
            .ToList();

        if (!string.IsNullOrWhiteSpace(defaultGameVersion))
		    SelectedGameVersion = defaultGameVersion;
        else
            SelectedGameVersion = GameVersions.FirstOrDefault() ?? "";

        await UpdateLoaderVersions(defaultLoaderVersion);
    }

    private static int CompareMcVersions( string a, string b ) {
        var va = ParseMcVersion(a);
        var vb = ParseMcVersion(b);

        var len = Math.Max(va.Length, vb.Length);

        for(int i = 0; i < len; i++) {
            var pa = i < va.Length ? va[i] : 0;
            var pb = i < vb.Length ? vb[i] : 0;

            var cmp = pa.CompareTo(pb);
            if(cmp != 0)
                return cmp;
        }

        return 0;
    }

    private static int[] ParseMcVersion( string version ) {
        // elimină snapshot-uri / sufixe
        var clean = version.Split('-')[0];

        return clean
            .Split('.')
            .Select(p => int.TryParse(p, out var n) ? n : 0)
            .ToArray();
    }

    public async Task UpdateLoaderVersions( string? defaultLoaderVersion = null ) {
        if(string.IsNullOrEmpty(SelectedGameVersion))
            return;

        var versions = await _apiService.GetLoaderVersionsAsync(SelectedLoader, SelectedGameVersion);
        LoaderVersions = versions;

        if(!string.IsNullOrWhiteSpace(defaultLoaderVersion))
            SelectedLoaderVersion = defaultLoaderVersion;
        else
            SelectedLoaderVersion = LoaderVersions.FirstOrDefault() ?? "";
    }

}
