using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using ModpackInstaller.ViewModels.Topbars;
using ModpackInstaller.ViewModels;
using ModpackInstaller.Windows;
using ModpackInstaller.Models;
using ModpackInstaller.Services.Modpack;
using ModpackInstaller.Infrastructure;
using Avalonia.VisualTree;

namespace ModpackInstaller.Views.Topbars;

public partial class GlobalTopBarView : UserControl
{
	public GlobalTopBarView()
	{
		InitializeComponent();
	}
}