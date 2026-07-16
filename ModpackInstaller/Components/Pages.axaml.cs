using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Metadata;
using Avalonia.VisualTree;

namespace ModpackInstaller.Components;

public class Pages : TemplatedControl {
    
    public static readonly DirectProperty<Pages, Page?> CurrentPageProperty =
        AvaloniaProperty.RegisterDirect<Pages, Page?>(
            nameof(CurrentPage),
            o => o.CurrentPage);

    public Page? CurrentPage => Items.Count > SelectedIndex ? Items[SelectedIndex] : null;
    
    [Content]
    public List<Page> Items { get; } = [];

    public static readonly StyledProperty<int> SelectedIndexProperty =
        AvaloniaProperty.Register<Pages, int>(nameof(SelectedIndex), defaultValue: 0);

    public int SelectedIndex {
        get => GetValue(SelectedIndexProperty);
        set {
            SetValue(SelectedIndexProperty, value);
            RaisePropertyChanged(CurrentPageProperty, null, CurrentPage);
        }
    }

    public void Next() {
        if (SelectedIndex < Items.Count - 1 && Items[SelectedIndex].Validate())
            SelectedIndex++;
    }

    public void Back() {
        if (SelectedIndex > 0)
            SelectedIndex--;
    }

    public void GoToTag(string tag) {
        var index = -1;
        for (int i = 0; i < Items.Count; i++)
            if (Items[i].PageTag == tag) index = i;
        
        if (index != -1) SelectedIndex = index;
    }
}

public class Page : StackPanel {
    public static readonly StyledProperty<string?> PageTagProperty =
        AvaloniaProperty.Register<Page, string?>(nameof(PageTag));

    public string? PageTag { get => GetValue(PageTagProperty); set => SetValue(PageTagProperty, value); }
    
    public static readonly StyledProperty<Func<bool>?> ValidatorProperty =
        AvaloniaProperty.Register<Page, Func<bool>?>(nameof(Validator));

    public Func<bool>? Validator {
        get => GetValue(ValidatorProperty);
        set => SetValue(ValidatorProperty, value);
    }

    public virtual bool Validate() {
        return Validator?.Invoke() ?? true;
    }
}

public class PageNextButton : Button {
    public PageNextButton() {
        Content = "Next";
    }

    protected override void OnClick() {
        this.FindAncestorOfType<Pages>()?.Next();
        base.OnClick();
    }
}

public class PageBackButton : Button {
    public PageBackButton() {
        Content = "Back";
    }
    
    protected override void OnClick() {
        this.FindAncestorOfType<Pages>()?.Back();
        base.OnClick();
    }
}

public class PageSelectButton : Button {
    public static readonly StyledProperty<string?> TargetTagProperty =
        AvaloniaProperty.Register<PageSelectButton, string?>(nameof(TargetTag));

    public string? TargetTag {
        get => GetValue(TargetTagProperty);
        set => SetValue(TargetTagProperty, value);
    }

    protected override void OnClick() {
        var pages = this.FindAncestorOfType<Pages>();

        if (pages != null && TargetTag != null)
            pages.GoToTag(TargetTag);

        base.OnClick();
    }
}

public class PageNavigation : TemplatedControl {
    // Metodă helper apelată de butoane
    internal void NavigateNext() => this.FindAncestorOfType<Pages>()?.Next();
    internal void NavigateBack() => this.FindAncestorOfType<Pages>()?.Back();
}