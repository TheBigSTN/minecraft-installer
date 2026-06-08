using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ModpackInstaller.Services;
using ModpackInstaller.Services.Modpack;

namespace ModpackInstaller.Desktop;
public static class CliRunner {
	public static async Task<int> RunAsync( string[] args ) {
		var command = args[0].ToLowerInvariant();

		try {
			switch(command) {
				case "help":
				case "-help":
				case "--help":
				case "-h":
					ShowHelp();
					return 0;

				case "discover":
					await DiscoverAsync();
					return 0;

				case "install":
					if(args.Length < 2) {
						Console.WriteLine("Missing modpack id.");
						return 1;
					}

					await InstallAsync(args[1]);
					return 0;

                case "server-install":
                    // Logica pentru server
                    await ServerInstallAsync(args[1]);
                    return 0;

                case "update":
					await UpdateAsync();
					return 0;

				default:
					Console.WriteLine($"Unknown command: {command}");
					ShowHelp();
					return 1;
			}
		} catch(Exception ex) {
			Console.WriteLine(ex);
			return -1;
		}
	}

	private static async Task DiscoverAsync() {
		var modpacks = await BackendApiService.GetPublicModpacksAsync();
		if(modpacks == null)
			return;

		foreach(var mp in modpacks) {
			Console.WriteLine($"{mp.Id} - {mp.ModpackName}");
		}
	}

	private static async Task InstallAsync( string modpackId ) {
		var modpacks = await BackendApiService.GetPublicModpacksAsync();
		if(modpacks == null)
			return;

		var modpack = modpacks.FirstOrDefault(x => x.Id == modpackId);

		if(modpack == null) {
			Console.WriteLine($"Modpack {modpackId} not found");
			return;
		}

		ModpackMedatataService metadataService = new();
		if(metadataService.Exists(modpackId)) {
			Console.WriteLine($"Modpack {modpackId} is already installed in {metadataService.Load(modpackId)!.InstallPath}");
			return;
		}
		
		var installPath = Environment.CurrentDirectory;

		var progress = new Progress<double>(p => {
			Console.Write($"\rProgress: {p:0.00}%");
		});

		static void status( string text ) {
			Console.WriteLine();
			Console.WriteLine(text);
		}

		await ModpackInstallService.DownloadAndInstallModpack(
			modpack,
			installPath,
			progress,
			status);

		Console.WriteLine();
		Console.WriteLine("Install complete.");
	}

    private static async Task ServerInstallAsync( string modpackId ) {
        var modpacks = await BackendApiService.GetPublicModpacksAsync();
        if(modpacks == null)
            return;

        var modpack = modpacks.FirstOrDefault(x => x.Id == modpackId);

        if(modpack == null) {
            Console.WriteLine($"Modpack {modpackId} not found");
            return;
        }

        ModpackMedatataService metadataService = new();
        if(metadataService.Exists(modpackId)) {
            Console.WriteLine($"Modpack {modpackId} is already installed in {metadataService.Load(modpackId)!.InstallPath}");
            return;
        }

        var installPath = Path.Combine(Environment.CurrentDirectory, "..");

        var progress = new Progress<double>(p => {
            Console.Write($"\rProgress: {p:0.00}%");
        });

        static void status( string text ) {
            Console.WriteLine();
            Console.WriteLine(text);
        }

        await ModpackInstallService.DownloadAndInstallModpackServer(
            modpack,
            installPath,
            progress,
            status);

        Console.WriteLine();
        Console.WriteLine("Install complete.");
    }

    private static async Task UpdateAsync() {
		var installPath = Environment.CurrentDirectory;

		var metadataService = new ModpackMedatataService();

		var metadata = metadataService.Load(installPath);

		if (metadata == null) {
			Console.WriteLine("No modpack installed in the current directory.");
			return;
		}

		await ModpackInstallService.UpdateModpack(metadata);

		Console.WriteLine("Update completed.");
	}

    private static void ShowHelp() {
        Console.WriteLine("Launcher CLI (WIP)");
        Console.WriteLine();
        Console.WriteLine("Commands:");
        Console.WriteLine("  discover                    => List all publicly available modpacks");
        Console.WriteLine("  install <id> [path]         => Install a modpack in the current folder or a specific path");
        Console.WriteLine("  server-install <id> [path]  => Install only server-side files in a specific path");
        Console.WriteLine("  update                      => Update the modpack in the current directory");
        Console.WriteLine("  help                        => Show this help page");
        Console.WriteLine();
        Console.WriteLine("Examples:");
        Console.WriteLine("  Launcher install <uuid>");
        Console.WriteLine("  Launcher server-install <uuid>");
    }
}