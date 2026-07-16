using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Media;
using Avalonia.Metadata;

namespace ModpackInstaller.Components;

public class Dialog : TemplatedControl {
    [Content]
    public List<Control> Children { get; } = [];
    
    protected override void OnApplyTemplate(TemplateAppliedEventArgs e) {
        base.OnApplyTemplate(e);

        var header = e.NameScope.Find<Grid>("HeaderContainer");
        var body = e.NameScope.Find<Grid>("BodyContainer");
        var sidebar = e.NameScope.Find<Grid>("SidebarContainter");

        if (header == null || body == null || sidebar == null)
            return;

        DialogSidebar? generatedSidebar = null;
        DialogContent? generatedContent = null;

        foreach (var child in Children) {
            switch (child) {
                case DialogHeader:
                    header.Children.Add(child);
                    break;

                case DialogFooter:
                    Grid.SetRow(child, 2);
                    body.Children.Add(child);
                    break;

                case DialogSidebar:
                    sidebar.Children.Add(child);
                    break;

                case DialogContent:
                    body.Children.Add(child);
                    break;

                case DialogPage:
                    generatedSidebar ??= new DialogSidebar();
                    generatedContent ??= new DialogContent();
                    break;

                default:
                    throw new InvalidOperationException(
                        $"Unsupported dialog child: {child.GetType().Name}");
            }
        }

        if (generatedSidebar == null || generatedContent == null)
            return;

        sidebar.Children.Add(generatedSidebar);
        body.Children.Add(generatedContent);

        foreach (var page in Children.OfType<DialogPage>()) {
            var tab = new DialogSidebarTab {
                Content = page.Header,
                IconPath = page.Icon
            };

            tab.IsCheckedChanged += (_, _) => {
                if (tab.IsChecked != true)
                    return;

                generatedContent.Children.Clear();
                generatedContent.Children.Add(page);
            };

            generatedSidebar.Children.Add(tab);

            if (generatedSidebar.Children.Count == 1)
                tab.IsChecked = true;
        }
    }
}

public class DialogHeader : Grid {
    public DialogHeader() {
        ColumnDefinitions = new ColumnDefinitions("*,Auto");
        Margin = new Thickness(28, 24, 24, 20);
    }
}

public class DialogContent : StackPanel;

public class DialogFooter : StackPanel;

public class DialogSidebar : StackPanel;

public class DialogSeparator : Border;

public class DialogCloseButton : Button {
    public DialogCloseButton() {
        Grid.SetColumn(this, 1);
            
        Content = new Avalonia.Svg.Svg(new Uri("avares://ModpackInstaller/Assets/x.svg")) {
            Width = 30,
            Height = 30,
            Path = "/Assets/x.svg"
        };
    }
}

public class DialogTitle : TextBlock {
    public DialogTitle() {
        FontSize = 28;
        FontWeight = FontWeight.SemiBold;
    }
}

public class DialogDescription : TextBlock {
    public DialogDescription() {
        Opacity = .7;
        TextWrapping = TextWrapping.Wrap;
    }
}

public class DialogSidebarTab : RadioButton {
    public static readonly StyledProperty<string?> IconPathProperty =
        AvaloniaProperty.Register<DialogSidebarTab, string?>(nameof(IconPath));

    public string? IconPath {
        get => GetValue(IconPathProperty);
        set => SetValue(IconPathProperty, value);
    }
}

public class DialogPage : StackPanel
{
    public static readonly StyledProperty<string?> HeaderProperty =
        AvaloniaProperty.Register<DialogPage, string?>(nameof(Header));

    public static readonly StyledProperty<string?> IconProperty =
        AvaloniaProperty.Register<DialogPage, string?>(nameof(Icon));

    public string? Header
    {
        get => GetValue(HeaderProperty);
        set => SetValue(HeaderProperty, value);
    }

    public string? Icon
    {
        get => GetValue(IconProperty);
        set => SetValue(IconProperty, value);
    }
}