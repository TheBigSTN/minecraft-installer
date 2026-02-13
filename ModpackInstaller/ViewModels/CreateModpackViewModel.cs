using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Text;
using System.Threading.Tasks;
using ModpackInstaller.Models;
using ModpackInstaller.Services;
using ReactiveUI;

namespace ModpackInstaller.ViewModels;

public class CreateModpackViewModel : ViewModelBase {
	private readonly ModrinthApiService _apiService = new();

	public event Action<ModpackMetadata?>? CloseRequested;
	public string BaseInstallPath { get; set; }
    public string Name { get; set; } = "";

	private string _selectedGameVersion = "";
	public string SelectedGameVersion {
		get => _selectedGameVersion;
		set {
			this.RaiseAndSetIfChanged(ref _selectedGameVersion, value);
			UpdateLoaderVersions();
		}
	}

	private IEnumerable<string> _gameVersions = [];
	public IEnumerable<string> GameVersions {
		get => _gameVersions;
		private set => this.RaiseAndSetIfChanged(ref _gameVersions, value);
	}

	private IEnumerable<string> _loaderVersions = [];
	public IEnumerable<string> LoaderVersions {
		get => _loaderVersions;
		private set => this.RaiseAndSetIfChanged(ref _loaderVersions, value);
	}
	
	private string _selectedLoaderVersion = "";
	public string SelectedLoaderVersion {
		get => _selectedLoaderVersion;
		set => this.RaiseAndSetIfChanged(ref _selectedLoaderVersion, value);
	}

	private ModLoaderType _selectedLoader;
	public ModLoaderType SelectedLoader {
		get => _selectedLoader;
		set {
			this.RaiseAndSetIfChanged(ref _selectedLoader, value);
			LoadGameVersions();

		}
	}
	public IEnumerable<ModLoaderType> Loaders => Enum.GetValues<ModLoaderType>();

	public ReactiveCommand<Unit, Unit> Create { get; }
	public ReactiveCommand<Unit, Unit> Cancel { get; }

	public CreateModpackViewModel(string BaseInstallPath) {
		//if (BaseInstallPath) BaseInstallPath = "";
		this.BaseInstallPath = BaseInstallPath;


        Create = ReactiveCommand.Create(() => {
			if (string.IsNullOrWhiteSpace(Name))
				return;

			var metadata = new ModpackMetadata {
				Id = Guid.NewGuid().ToString("N"),
				Name = Name.Trim(),
				GameVersion = SelectedGameVersion,
				Loader = SelectedLoader,
				LoaderVersion = SelectedLoaderVersion,
				InstallPath = Path.Combine(BaseInstallPath, Name.Trim())
			};

			CloseRequested?.Invoke(metadata);
		});

		Cancel = ReactiveCommand.Create(() => {
			CloseRequested?.Invoke(null);
		});

		// 🔹 Încarcă game versions la startup
		LoadGameVersions();
	}

	private async void LoadGameVersions() {
		var versions = await _apiService.GetGameVersionsAsync(SelectedLoader);

        GameVersions = versions
			// ❌ scoate placeholder-ele Fabric / Quilt
			.Where(v =>
				!string.IsNullOrWhiteSpace(v) &&
				!v.StartsWith("$") &&
				!v.Contains("{") &&
				!v.Contains("}"))
			// 🔽 sortare semantică, DESC (cele mai noi sus)
			.OrderByDescending(v => v, Comparer<string>.Create(CompareMcVersions))
			.ToList();

        // set default dacă e gol
        SelectedGameVersion = GameVersions.FirstOrDefault() ?? "";

		// când schimb loader sau game version, actualizează loader versions
		UpdateLoaderVersions();
	}

    private static int CompareMcVersions(string a, string b) {
        var va = ParseMcVersion(a);
        var vb = ParseMcVersion(b);

        var len = Math.Max(va.Length, vb.Length);

        for (int i = 0; i < len; i++) {
            var pa = i < va.Length ? va[i] : 0;
            var pb = i < vb.Length ? vb[i] : 0;

            var cmp = pa.CompareTo(pb);
            if (cmp != 0)
                return cmp;
        }

        return 0;
    }

    private static int[] ParseMcVersion(string version) {
        // elimină snapshot-uri / sufixe
        var clean = version.Split('-')[0];

        return clean
            .Split('.')
            .Select(p => int.TryParse(p, out var n) ? n : 0)
            .ToArray();
    }

    public async void UpdateLoaderVersions() {
		if (string.IsNullOrEmpty(SelectedGameVersion))
			return;

		var versions = await _apiService.GetLoaderVersionsAsync(SelectedLoader, SelectedGameVersion);
		LoaderVersions = versions;

		SelectedLoaderVersion = LoaderVersions.FirstOrDefault() ?? "";
	}
}
