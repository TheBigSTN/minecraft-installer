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

namespace ModpackInstaller.Services;

public interface IDialogService {
	Task<ButtonResult> EmitSimpleDialog(MessageBoxStandardParams messageBoxParams);
	Task EmitSimpleOkDialog(string title, string message);
}

public class DialogService : IDialogService {

	public async Task<ButtonResult> EmitSimpleDialog(MessageBoxStandardParams messageBoxParams) {

		var messageBox = MessageBoxManager.GetMessageBoxStandard(messageBoxParams);
		var result = await messageBox.ShowWindowAsync();

		return result;
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

}