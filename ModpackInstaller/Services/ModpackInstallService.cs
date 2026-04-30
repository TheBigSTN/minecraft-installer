using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ModpackInstaller.Infrastructure;
using ModpackInstaller.Models;
using ModpackInstaller.Models.DTOs;
using ModpackInstaller.Services.Modpack;
using System.Text.Json;
using System.Diagnostics;
using System.Threading;

namespace ModpackInstaller.Services;

public class PatchManifest {
	public List<InstalledModInfo> Added { get; set; } = new();
	public List<InstalledModInfo> Removed { get; set; } = new();
	public bool Empty { get; set; }
}

public class ModpackInstallService {
    public static async Task InstallModsOfModpack( ModpackMetadata modpackMetadata, IProgress<double>? progress = null ) {
        ModpackManifestService manifestService = new(modpackMetadata.InstallPath);
        ModpackManifest manifest = manifestService.Load();

        if(manifest.InstalledMods.Count == 0) {
            progress?.Report(100);
            return;
        }

        int totalMods = manifest.InstalledMods.Count;
        int downloadedMods = 0;

        // Mapăm fiecare download la un task care, la finalizare, incrementează contorul
        var downloadTasks = manifest.InstalledMods.Select(async mod => {
            await ModpackManifestService.DownloadModAsync(mod, modpackMetadata.InstallPath);

            // Incrementăm în mod thread-safe
            Interlocked.Increment(ref downloadedMods);

            // Raportăm progresul: (moduri gata / total moduri) * 100
            double currentProgress = (double)downloadedMods / totalMods * 100;
            progress?.Report(currentProgress);
        });

        await Task.WhenAll(downloadTasks);
    }
    //public static async Task InstallModsOfModpack(ModpackMetadata modpackMetadata) {
    //	ModpackManifestService manifestService = new(modpackMetadata.InstallPath);
    //	ModpackManifest manifest = manifestService.Load();

    //	await Task.WhenAll(
    //		 manifest.InstalledMods
    //			 .Select(mod => ModpackManifestService.DownloadModAsync(mod, modpackMetadata.InstallPath))
    //	 );

    //}
    public static async Task<ModpackMetadata> DownloadAndInstallModpack(
		PublicModpackRequestResponse modpack, 
		string baseInstallPath,
        IProgress<double>? progress = null,
        Action<string>? statusUpdate = null ) {
		var installPath = Path.Combine(baseInstallPath, modpack.ModpackName.Trim());

		// Creează folderul dacă nu există
		Directory.CreateDirectory(installPath);

		ModpackMetadata metadata = new() {
			Id = modpack.Id,
			InstallPath = installPath,
			Name = modpack.ModpackName,
			GameVersion = modpack.GameVersion,
			Loader = modpack.Loader,
			Author = modpack.AuthorName,
			CreatedAt = modpack.CreatedAt,
			UpdatedAt = modpack.ModifiedAt,
			Description = "",
			IsPublic = true,
			LoaderVersion = modpack.LoaderVersion,
			ModpackPassword = null,
			OwnerNickname = modpack.AuthorName,
			SharingCode = null,
			Version = modpack.LatestVersion,
			Source = ModpackSource.Remote
		};

        statusUpdate?.Invoke("Se descarcă modpack-ul...");
        var zipPath = Path.Combine(installPath, "modpack.zip");
		try {

			await ModpackApiService.DownloadVersionAsync(
				modpack.Id,
				modpack.LatestVersion,
				zipPath,
				progress
			);
            ZipFile.ExtractToDirectory(zipPath, installPath, true);
		}
		finally {
			if (File.Exists(zipPath))
				File.Delete(zipPath);
		}

        statusUpdate?.Invoke("Se descarcă modurile...");
        progress?.Report(0);
        // instanță ModpackMedatataService cu path-ul principal
        var registry = new ModpackMedatataService();

		ModpackManifestService modpackManifestService = new(metadata.InstallPath);

		//modpackManifestService.ParseAllMods(modInfo => modInfo.Source = ModSource.Remote);

		// creez / salvez metadata
		registry.Create(metadata);

		await InstallModsOfModpack(metadata, progress);
		return metadata;
	}

	public static async Task UpdateModpack(ModpackMetadata modpackMetadata) {
		ModpackManifestService modpackManifestService = new(modpackMetadata.InstallPath);

		var patchZipPath = Path.Combine(modpackMetadata.InstallPath, "patch-zip.zip");
		var patchUnzipPath = Path.Combine(modpackMetadata.InstallPath, "unzipped-patch");

		if (!Directory.Exists(modpackMetadata.InstallPath))
			Directory.CreateDirectory(modpackMetadata.InstallPath);

		try {
			var modpackInfo = await ModpackApiService.GetMetadataAsync(modpackMetadata.Id, null) ?? throw new Exception("Modpack not found on server.");
			await ModpackApiService.DownloadPatchAsync(
				modpackMetadata.Id,
				modpackMetadata.Version,
				modpackInfo.LatestVersion,
				patchZipPath);

			ZipFile.ExtractToDirectory(patchZipPath, patchUnzipPath, true);

			await ApplyPatch(patchUnzipPath, modpackMetadata.InstallPath, modpackManifestService, modpackMetadata.InstallPath);

			modpackMetadata.Version = modpackInfo.LatestVersion;

			new ModpackMedatataService().Save(modpackMetadata);
		}
		finally {
			if (File.Exists(patchZipPath))
				File.Delete(patchZipPath);

			if (Directory.Exists(patchUnzipPath))
				Directory.Delete(patchUnzipPath, true);
		}
	}

	private static async Task ApplyPatch(
		string patchFolder,
		string installPath,
		ModpackManifestService manifestService,
		string modpackInstallPath) {
		// 1️⃣ DELETE
		var deleteFile = Path.Combine(patchFolder, "delete.txt");
		if (File.Exists(deleteFile)) {
			var lines = File.ReadAllLines(deleteFile);

			foreach (var relativePath in lines) {
				if (string.IsNullOrWhiteSpace(relativePath))
					continue;

				var fullPath = Path.Combine(installPath, relativePath.Trim());

				if (File.Exists(fullPath))
					File.Delete(fullPath);

				if (Directory.Exists(fullPath))
					Directory.Delete(fullPath, true);
			}
		}

		// 2️⃣ COPY FILES (exclude special files)
		foreach (var file in Directory.GetFiles(patchFolder, "*", SearchOption.AllDirectories)) {
			var relative = Path.GetRelativePath(patchFolder, file);

			if (relative == "delete.txt" || relative == "patch-manifest.json")
				continue;

			var destPath = Path.Combine(installPath, relative);

			Directory.CreateDirectory(Path.GetDirectoryName(destPath)!);
			File.Copy(file, destPath, true);
		}

		// 3️⃣ APPLY PATCH MANIFEST
		var manifestFile = Path.Combine(patchFolder, "patch-manifest.json");
		if (File.Exists(manifestFile)) {
			var json = File.ReadAllText(manifestFile);
			var patchManifest = JsonSerializer.Deserialize<PatchManifest>(json, AppVariables.WebJsonOptions);

			if (patchManifest != null) {
				// 1️⃣ Șterge mod-urile eliminate
				foreach (var mod in patchManifest.Removed)
					manifestService.RemoveMod(mod.ProjectId);

				// 2️⃣ Adaugă / actualizează mod-urile noi
				foreach (var mod in patchManifest.Added) {
					// Verificăm dacă trebuie să adăugăm / actualizăm
					if (manifestService.AddOrUpdateMod(mod, true)) {
						// Descarcă și verifică succesul
						bool success = await ModpackManifestService.DownloadModAsync(mod, modpackInstallPath);

						if (!success) {
							// Dacă download-ul eșuează → eliminăm din manifest
							manifestService.RemoveMod(mod.ProjectId);
							Debug.WriteLine($"Mod-ul {mod.Title} nu a fost instalat din cauza unei erori.");
						}
					}
				}

				// 3️⃣ Salvăm manifestul final
				manifestService.Save();
			}
		}
	}
}

