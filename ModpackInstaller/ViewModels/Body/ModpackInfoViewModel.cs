using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using ModpackInstaller.Infrastructure;
using ModpackInstaller.Models;
using ModpackInstaller.Services.Modpack;
using ReactiveUI;

namespace ModpackInstaller.ViewModels.Body;
public enum ModpackExportMode {
    LocalZip,
    Unlisted,
    Public
}


public class ModpackInfoViewModel : ViewModelBase {

	public ModpackMetadata? Modpack { get; set; }

	private readonly MainViewModel _main;
    private readonly ModpackMedatataService _medatataService = new(AppVariables.InstallerRoot);

    public Interaction<Unit, ModpackExportMode?> ShowExportDialog { get; }
    = new();

    public ReactiveCommand<Unit, Unit> PBUpdateModpackCommand { get; }
    public ReactiveCommand<Unit, Unit> EditModpackCommand { get; }
	public ReactiveCommand<Unit, Unit> ExportModpackCommand { get; }
	public ReactiveCommand<Unit, Unit> DeleteModpackCommand { get; }

	public ModpackInfoViewModel(ModpackMetadata? modpack, MainViewModel main) {
		Modpack = modpack!;
		_main = main;

        var canUpdate = this.WhenAnyValue(
            x => x.Modpack,
            (ModpackMetadata? mp) => mp != null && !string.IsNullOrEmpty(mp.ModpackPassword)
        );

        PBUpdateModpackCommand = ReactiveCommand.CreateFromTask(async () => {
            try {
                ModpackPublicizeService publicizeService = new(Modpack);

                //// 1. Actualizăm Metadata (Nume, Descriere, versiune joc, etc.)
                //// Nu are sens deoarece server ul modifica singur versiunea
                //await publicizeService.UpdateMetadataAsync();

                // 2. Incrementăm versiunea locală pentru noul fișier
                Modpack.Version++;

                _medatataService.Save(Modpack);

                // 3. Urcăm noul ZIP pentru noua versiune
                await publicizeService.UploadNewVersionAsync();

                // 4. Salvăm modificările (noua versiune) local pe disc
                // ModpackMetadataService.Save(Modpack);

                Console.WriteLine("Update realizat cu succes!");
            }
            catch (Exception ex) {
                // Aici ar trebui să trimiți eroarea către un dialog în UI
                Console.WriteLine($"Eroare la update: {ex.Message}");
            }
        }, canUpdate);
		
		EditModpackCommand = ReactiveCommand.Create(() =>
		{
			if (Modpack == null) return;

			// deschide pagina / dialogul cu mods
			_main.EditModpack(modpack);
		});

        ExportModpackCommand = ReactiveCommand.CreateFromTask(async () =>
        {
			string zipExportPath = Path.Combine(AppVariables.InstallerRoot, "exports", $"{Modpack.Name}_{DateTime.Now:yy-MM-dd-HH-mm-ss}.zip");

            var result = await ShowExportDialog.Handle(Unit.Default);

            if (result == null)
                return;

			ModpackPublicizeService modpackPublicize = new(modpack);

            switch (result) {
                case ModpackExportMode.LocalZip:
                    ModpackPackageService.ExportFullAsync(modpack.InstallPath, zipExportPath);
                    break;

                case ModpackExportMode.Unlisted:
                    await modpackPublicize.CreateOnServerAsync(false, ModpackPublicizeService.GenerateCode());
                    break;

                case ModpackExportMode.Public:
                    await modpackPublicize.CreateOnServerAsync(true);
                    break;
            }
        });

        DeleteModpackCommand = ReactiveCommand.Create(() =>
		{
			if (Modpack == null) return;

			var registry = new ModpackMedatataService(AppVariables.InstallerRoot);
			registry.Delete(Modpack.Id);

			try {
				if (!string.IsNullOrEmpty(Modpack.InstallPath) && Directory.Exists(Modpack.InstallPath)) {
					// 'true' indică ștergerea recursivă (tot ce e în folder)
					Directory.Delete(Modpack.InstallPath, true);
				}
			}
			catch (Exception ex) {
				// Poate fi blocat de un proces (ex: Minecraft deschis)
				Debug.WriteLine($"Nu s-a putut șterge folderul: {ex.Message}");
			}

			_main.RefreshModpackList();
			_main.ShowGlobal();
		});
	}

	// helperi pt UI (opțional, dar util)
	public bool HasModpack => Modpack != null;
}