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

        var knownFlags = new HashSet<string>
				{
			"--server",
			"--non-discoverable"
		};

        var flags = new HashSet<string>();
        var positionals = new List<string>();

        for(int i = 1; i < args.Length; i++) {
            var arg = args[i];

            if(arg.StartsWith("--")) {
                if(!knownFlags.Contains(arg)) {
                    Console.WriteLine($"Unknown flag: {arg}");
                    return 1;
                }

                flags.Add(arg);
            } else {
                positionals.Add(arg);
            }
        }

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
                    if(positionals.Count < 1) {
                        Console.WriteLine("Missing modpack id.");
                        return 1;
                    }

                    var modpackId = positionals[0];

                    var isServer = flags.Contains("--server");
                    var nonDiscoverable = flags.Contains("--non-discoverable");

                    await InstallAsync(modpackId, isServer, nonDiscoverable);
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

	private static async Task InstallAsync( 
			string modpackId,
			bool isServer,
			bool nonDiscoverable
		) {
		var modpacks = await BackendApiService.GetPublicModpacksAsync();
		if(modpacks == null)
			return;

		var modpack = modpacks.FirstOrDefault(x => x.Id == modpackId);

		if(modpack == null) {
			Console.WriteLine($"Modpack {modpackId} not found");
			return;
		}

		ModpackMedatataService metadataService = new();
		if(metadataService.Exists(modpackId) && !nonDiscoverable) {
			Console.WriteLine($"Modpack {modpackId} is already installed globaly");
            Console.WriteLine($"If you still wish to install it  use the --non-discoverable flag");
            Console.WriteLine($"That installs the modpack but you can't use the GUI to modify it");
            Console.WriteLine($"You have to use the CLI");
            return;
		}

        ModpackMedatataService localMetadataService = new(Environment.CurrentDirectory);
        if( localMetadataService.Exists() ) {
            Console.WriteLine("A modpack is already installed in the current directory");
            Console.WriteLine("Please choose a different directory or uninstall the existing modpack");
            return;
        }

		Console.WriteLine($"Installing modpack {modpack.ModpackName}");

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
			status,
			isServer,
			nonDiscoverable,
			false);

		Console.WriteLine();
		Console.WriteLine("Install complete.");
	}

    private static async Task UpdateAsync() {
		var installPath = Environment.CurrentDirectory;

		var metadataService = new ModpackMedatataService(installPath);

		var metadata = metadataService.Load();

		await ModpackInstallService.UpdateModpack(metadata);

		Console.WriteLine("Update completed.");
	}

    private static void ShowHelp() {
        Console.WriteLine("Modpack Installer CLI");
        Console.WriteLine();
        Console.WriteLine("Usage:");
        Console.WriteLine("  modpack-installer <command> [options]");
        Console.WriteLine();

        Console.WriteLine("Commands:");
        Console.WriteLine("  discover");
        Console.WriteLine("      List all publicly available modpacks.");
        Console.WriteLine();

        Console.WriteLine("  install <modpack-id> [options]");
        Console.WriteLine("      Install a modpack into the current directory.");
        Console.WriteLine();
        Console.WriteLine("      Options:");
        Console.WriteLine("        --server");
        Console.WriteLine("            Install server files only.");
        Console.WriteLine();
        Console.WriteLine("        --non-discoverable");
        Console.WriteLine("            Install without registering the modpack globally.");
        Console.WriteLine("            The installation will not appear in the GUI.");
        Console.WriteLine("            Management must be done through the CLI.");
        Console.WriteLine();

        Console.WriteLine("  update");
        Console.WriteLine("      Update the modpack installed in the current directory.");
        Console.WriteLine();

        Console.WriteLine("  help");
        Console.WriteLine("      Show this help page.");
        Console.WriteLine();

        //Console.WriteLine("Examples:");
        //Console.WriteLine("  modpack-installer discover");
        //Console.WriteLine("  modpack-installer install my-modpack");
        //Console.WriteLine("  modpack-installer install my-modpack --server");
        //Console.WriteLine("  modpack-installer install my-modpack --non-discoverable");
        //Console.WriteLine("  modpack-installer update");
    }
}