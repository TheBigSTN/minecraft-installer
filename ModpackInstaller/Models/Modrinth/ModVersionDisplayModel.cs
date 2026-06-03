using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ReactiveUI;
using ReactiveUI.SourceGenerators;

namespace ModpackInstaller.Models.Modrinth;
public partial class ModVersionDisplayModel : ReactiveObject {

    [Reactive]
    private ModrinthVersionExtended _modrinthVersion;
    [Reactive]
    private bool _isUpgrade;
    [Reactive]
    private bool _isDowngrade;
    [Reactive]
    private bool _isCurrent;

    public ModVersionDisplayModel( ModrinthVersionExtended modrinthVersion ) {
        _modrinthVersion = modrinthVersion;

        this.WhenAnyValue(x => x.IsUpgrade, x => x.IsDowngrade)
            .Subscribe(_ => {
                this.RaisePropertyChanged(nameof(ArrowSymbol));
                this.RaisePropertyChanged(nameof(ArrowColor));
            });
    }

    public string ArrowSymbol => IsUpgrade ? "▲" : (IsDowngrade ? "▼" : "=");
    public string ArrowColor => IsUpgrade ? "Green" : (IsDowngrade ? "Red" : "Gray");
}