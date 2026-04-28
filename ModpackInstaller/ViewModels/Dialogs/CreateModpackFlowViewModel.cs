using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Text;
using System.Threading.Tasks;
using ModpackInstaller.Infrastructure;
using ModpackInstaller.Models;
using ModpackInstaller.Services;
using ModpackInstaller.Views;
using ReactiveUI;

namespace ModpackInstaller.ViewModels.Dialogs;
public class CreateModpackFlowViewModel : ViewModelBase {
	private readonly ModrinthApiService _apiService = new();

	public event Action<ModpackMetadata?>? CloseRequested;

	public CreateModpackFlowViewModel() {
		_currentStep = new SelectPlatformStepViewModel(this);

		Next = ReactiveCommand.Create(OnNext);
		Back = ReactiveCommand.Create(OnBack);
		Cancel = ReactiveCommand.Create(() => CloseRequested?.Invoke(null));
		Finish = ReactiveCommand.Create(OnFinish);
	}

	// ---------------- STEP ----------------

	private StepViewModelBase _currentStep;
	public StepViewModelBase CurrentStep {
		get => _currentStep;
		set => this.RaiseAndSetIfChanged(ref _currentStep, value);
	}

	// ---------------- PLATFORM ----------------

	public InstallPlatform SelectedModPlatform;

	// ---------------- CONFIG (Step 2) ----------------

	public string Name { get; set; }	= "";

	public ModLoaderType SelectedLoader;

	public string SelectedGameVersion	= "";

	public string SelectedLoaderVersion = "";


    // ---------------- NAVIGATION ----------------

    public ReactiveCommand<Unit, Unit> Next { get; }
	public ReactiveCommand<Unit, Unit> Back { get; }
	public ReactiveCommand<Unit, Unit> Cancel { get; }
	public ReactiveCommand<Unit, Unit> Finish { get; }

	private void OnNext() {
		if(CurrentStep is SelectPlatformStepViewModel) {
			CurrentStep = new ConfigureStepViewModel(this);
		} else if (CurrentStep is ConfigureStepViewModel) {
			CurrentStep = new ConfirmStepViewModel(this);
		}
	}

	private void OnBack() {
		if(CurrentStep is ConfirmStepViewModel) {
			CurrentStep = new ConfigureStepViewModel(this);
		}
		else if(CurrentStep is ConfigureStepViewModel)
			CurrentStep = new SelectPlatformStepViewModel(this);
	}

	private void OnFinish() {
		if(string.IsNullOrWhiteSpace(Name) || CurrentStep is not ConfirmStepViewModel)
			return;

		var metadata = new ModpackMetadata {
			Id = Guid.NewGuid().ToString("N"),
			Name = Name.Trim(),
			GameVersion = SelectedGameVersion,
			Loader = SelectedLoader,
			LoaderVersion = SelectedLoaderVersion,
			InstallPath = Path.Combine(AppVariables.GetBaseInstallPathFromLauncer(SelectedModPlatform), Name.Trim())
		};

		CloseRequested?.Invoke(metadata);
	}

    // ---------------- DATA LOADING ----------------

}
