using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Avalonia.Threading;
using ModpackInstaller.Models;
using ModpackInstaller.Models.Modrinth;
using ModpackInstaller.Services;
using ModpackInstaller.Services.Modpack;
using ModpackInstaller.ViewModels.Sidebars;
using ReactiveUI;

namespace ModpackInstaller.ViewModels.Body {
	public class ModrinthBrowserViewModel : ViewModelBase {
		private readonly ModpackMetadata _modpack;
		private readonly MainViewModel _main;
        private readonly ModpackManifestService _manifestService;

        public ObservableCollection<ModrinthProject> Mods { get; } = new();

        public ReactiveCommand<ModrinthProject, Unit> AddModCommand { get; }
        public string SearchQuery {
			get => _main.SearchQuery;
			set {
				_main.SearchQuery = value;
			}
		}

		public ModrinthBrowserViewModel(ModpackMetadata modpack, MainViewModel main) {
			_modpack = modpack;
			_main = main;
            _main.SearchQuery = "";

            _main.WhenAnyValue(x => x.SearchQuery)
                .Throttle(TimeSpan.FromMilliseconds(400), RxApp.MainThreadScheduler)
                // Permitem și căutarea goală pentru a vedea "featured mods" la început
                .Select(q => q?.Trim() ?? "")
                .DistinctUntilChanged()
                .Subscribe(async _ => await SearchAsync());

            _manifestService = new ModpackManifestService(modpack.InstallPath);

            AddModCommand = ReactiveCommand.CreateFromTask<ModrinthProject, Unit>(async (project) => {
                await InstallModRecursive(project.Id, null);

                return Unit.Default;
            });

            _main.SearchQuery = "";
        }
        private int _currentOffset = 0;
        private const int PageSize = 20;
        private bool _isSearching = false;

        public async Task SearchAsync(bool append = false) {
            if (_isSearching) return;
            _isSearching = true;

            try {
                if (!append) {
                    _currentOffset = 0;
                }

                var url = $"https://api.modrinth.com/v2/search" +
                          $"?query={Uri.EscapeDataString(SearchQuery ?? "")}" +
                          $"&offset={_currentOffset}" +
                          $"&limit={PageSize}" +
                          $"&facets=[" +
                              $"[\"categories:{_modpack.Loader}\"]," +
                              $"[\"game_versions:{_modpack.GameVersion}\"]," +
                              $"[\"project_type:mod\"]" +
                          $"]";

                var result = await WebService.GetJson<ModrinthSearchResponse>(url);

                if (result == null) return;

                if (!append) Mods.Clear();

                foreach (var mod in result.Hits) {
                    // Evităm duplicatele la adăugare
                    if (!Mods.Any(m => m.Id == mod.Id)) {
                        Mods.Add(mod);
                    }
                }

                _currentOffset += PageSize;
            }
            finally {
                _isSearching = false;
            }
        }

        private async Task InstallModRecursive(string? projectId, string? versionId) {
            var apiService = new ModrinthApiService();
            ModrinthVersion? targetVersion = null;

            // 1. Obținem Versiunea (cu prioritate pe versionId dacă există)
            if (!string.IsNullOrEmpty(versionId)) {
                targetVersion = await apiService.GetVersionAsync(versionId);
            }
            else if (!string.IsNullOrEmpty(projectId)) {
                targetVersion = await apiService.GetCompatibleVersionAsync(projectId, _modpack.GameVersion, _modpack.Loader);
            }

            if (targetVersion == null) return;

            // 2. Verificăm dacă e deja instalat (folosind ProjectId din versiunea găsită)
            // IMPORTANT: Modrinth returnează uneori ProjectId cu sau fără prefix, verifică consistența
            if (_manifestService.Manifest.InstalledMods.Any(m => m.ProjectId == targetVersion.ProjectId))
                return;

            // 3. Obținem datele Proiectului pentru metadate (Icon, Title)
            var targetProject = await apiService.GetProjectAsync(targetVersion.ProjectId);
            if (targetProject == null) return;

            // 4. Adăugăm în Manifest și pornim Download
            // Folosim obiectul returnat de AddMod direct, fără să îl mai căutăm cu .First()
            var installedInfo = _manifestService.AddMod(targetProject, targetVersion);

            if (installedInfo != null) {
                // Pornim download-ul în fundal
                _ = _manifestService.DownloadModAsync(installedInfo, _modpack.InstallPath);

                // Notificăm UI-ul că lista s-a schimbat
                Dispatcher.UIThread.Post(() => {
                    MessageBus.Current.SendMessage(new ManifestChangedMessage { Mod = installedInfo });
                });
            }

            // 5. Rezolvăm dependențele REQUIRED recursiv
            if (targetVersion.Dependencies != null) {
                foreach (var dep in targetVersion.Dependencies.Where(d => d.DependencyType == "required")) {
                    // Nu avem nevoie de await aici dacă vrem să se instaleze în paralel, 
                    // dar e mai sigur cu await pentru a nu bloca API-ul Modrinth
                    _ = InstallModRecursive(dep.ProjectId, dep.VersionId);
                }
            }
        }

    }
}
