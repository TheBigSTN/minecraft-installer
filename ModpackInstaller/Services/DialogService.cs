using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Controls;
using ModpackInstaller.ViewModels.Topbars;
using ModpackInstaller.ViewModels;
using Avalonia;
using ModpackInstaller.Services.Modpack;
using ModpackInstaller.Infrastructure;
using MsBox.Avalonia.Enums;
using System.Diagnostics;
using MsBox.Avalonia;
using MsBox.Avalonia.Dto;
using ModpackInstaller.Models;
using ModpackInstaller.Windows;
using ModpackInstaller.ViewModels.Dialogs;
using ModpackInstaller.ViewModels.Body;
using ModpackInstaller.Views;

namespace ModpackInstaller.Services;

public interface IDialogService {
	void AttachWindow(Window window);
	Task<ButtonResult> EmitSimpleDialog(MessageBoxStandardParams messageBoxParams);
	Task EmitSimpleOkDialog(string title, string message);
	Task<ButtonResult> ShowConfirmation(string title, string message);
	Task<ModpackMetadata?> ShowCreateModpackDialog(string installPath, string installTarget);
	Task<ModpackExportMode?> ShowExportModpackDialog(ModpackMetadata modpack );
	Task<List<string>> ShowFileExcludePicker( string path);


}

public class DialogService : IDialogService {
	private Window? _window;
	public void AttachWindow(Window window) {
		_window = window;
	}

	public async Task<ButtonResult> EmitSimpleDialog(MessageBoxStandardParams messageBoxParams) {
		
		var messageBox = MessageBoxManager.GetMessageBoxStandard(messageBoxParams);
		if (_window != null) {
			return await messageBox.ShowWindowDialogAsync(_window);
		}

		return await messageBox.ShowWindowAsync();
	}

	public async Task EmitSimpleOkDialog(string title, string message) {
		MessageBoxStandardParams messageBoxParams = new() {
			ButtonDefinitions = ButtonEnum.Ok,
			ContentTitle = title,
			ContentMessage = message,
			Icon = Icon.Info,
			WindowStartupLocation = WindowStartupLocation.CenterOwner,
		};

		await EmitSimpleDialog(messageBoxParams);

	}

	public async Task<ButtonResult> ShowConfirmation(string title, string message) {
		MessageBoxStandardParams messageBoxParams = new() {
			ButtonDefinitions = ButtonEnum.YesNo,
			ContentTitle = title,
			ContentMessage = message,
			Icon = Icon.Info,
			WindowStartupLocation = WindowStartupLocation.CenterOwner,
		};

		return await EmitSimpleDialog(messageBoxParams);
	}

	public async Task<ModpackMetadata?> ShowCreateModpackDialog(string installPath, string installTarget) {

		if (_window == null)
			return null;

        var vm = new CreateModpackFlowViewModel();

        var dialog = new CreateModpackFlowWindow {
            DataContext = vm
        };

        vm.CloseRequested += result => dialog.Close(result);

        ModpackMetadata? metadata = await dialog.ShowDialog<ModpackMetadata?>(_window);

        if (metadata == null)
			return null;

		return metadata;
	}

    public async Task<ModpackExportMode?> ShowExportModpackDialog( ModpackMetadata modpack ) {
        if(_window == null)
            return null;

        var dialogVm = new ExportModpackDialogViewModel(modpack);

        var dialog = new ExportModpackDialogView {
            DataContext = dialogVm
        };

        dialogVm.CloseRequested += result => dialog.Close(result);

        return await dialog.ShowDialog<ModpackExportMode?>(_window);
    }

    public async Task<List<string>> ShowFileExcludePicker(string path) {
        if(_window == null)
            return [];

        var vm = new ExportPickerViewModel(path);

        var dialog = new ExportPickerView {
            DataContext = vm
        };

        vm.CloseRequested += result => dialog.Close(result);

        return await dialog.ShowDialog<List<string>>(_window);
    }


}