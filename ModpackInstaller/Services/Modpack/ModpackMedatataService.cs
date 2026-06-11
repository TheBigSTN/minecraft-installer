using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using ModpackInstaller.Infrastructure;
using ModpackInstaller.Models;

namespace ModpackInstaller.Services.Modpack;

public class ModpackMedatataService {
	private readonly string _registryPath;
	private readonly bool _isNonStandardPath;

    /// <summary>
    /// Uses non discoverable custom metadata path
    /// </summary>
    public ModpackMedatataService(string registryPath) {
		_registryPath = registryPath;
		_isNonStandardPath = true;
		Directory.CreateDirectory(_registryPath);
	}

	/// <summary>
	/// Uses default AppVariables.InstallerRoot
	/// </summary>
	public ModpackMedatataService() {
		_registryPath = Path.Combine(AppVariables.InstallerRoot, "modpacks");
        _isNonStandardPath = false;
        Directory.CreateDirectory(_registryPath);
	}

	// 📌 Creează / înregistrează metadata
	public ModpackMetadata Create(ModpackMetadata metadata) {
		metadata.CreatedAt = DateTime.UtcNow;
		metadata.UpdatedAt = metadata.CreatedAt;

		return Save(metadata);
	}

	// 📌 Salvează metadata extern
	public ModpackMetadata Save(ModpackMetadata metadata) {

        metadata.UpdatedAt = DateTime.UtcNow;

		var path = GetMetadataPath(metadata.Id);
		var json = JsonSerializer.Serialize(metadata, AppVariables.DefaultJsonOptions);
		File.WriteAllText(path, json);
		return metadata;
	}

    // 📌 Încearcă să încarce metadata (fără erori)
    public bool TryLoad(string id, out ModpackMetadata? metadata) {
		metadata = null;
		var path = GetMetadataPath(id);

		if (!File.Exists(path))
			return false;

		try {
			metadata = JsonSerializer.Deserialize<ModpackMetadata>(
				File.ReadAllText(path)
			);
			return metadata != null;
		}
		catch {
			return false;
		}
	}

	// 📌 Load direct (poate întoarce null)
	public ModpackMetadata? Load(string id) {
		TryLoad(id, out var metadata);
		return metadata;
	}

    public ModpackMetadata Load() {
        if(!_isNonStandardPath)
            throw new Exception("This is unavaliable when using a standard registry path.");

        TryLoad("in this context this value does not matter", out var metadata);

		if(metadata == null)
			throw new Exception("Metadata does not exist");

        return metadata;
    }

    // 📌 Există în registry
    public bool Exists(string id) {
        if(_isNonStandardPath)
            throw new Exception("This is unavaliable when using a non-standard registry path.");

        return File.Exists(GetMetadataPath(id));
	}

    public bool Exists() {
        if(!_isNonStandardPath)
            throw new Exception("This is unavaliable when using a standard registry path.");
        return File.Exists(GetMetadataPath("This value does not matter in this context"));
	}

    // 📌 Update parțial, safe
    public bool Update(string id, Action<ModpackMetadata> update) {
		if (!TryLoad(id, out var metadata) || metadata == null)
			return false;

        metadata.UpdatedAt = DateTime.UtcNow;

        update(metadata);
		Save(metadata);
		return true;
	}

	// 📌 Șterge metadata
	public bool Delete(string id) {
		var path = GetMetadataPath(id);
		if (!File.Exists(path))
			return false;

		File.Delete(path);
		return true;
	}

	// 📌 Enumeră TOATE modpack-urile din registry
	public IEnumerable<ModpackMetadata> LoadAll() {
		if (_isNonStandardPath)
			throw new Exception("This is unavaliable when using a non-standard registry path.");

        foreach (var file in Directory.GetFiles(_registryPath, "*.json")) {
			ModpackMetadata? metadata = null;
			try {
				metadata = JsonSerializer.Deserialize<ModpackMetadata>(
					File.ReadAllText(file)
				);
			}
			catch { }

			if (metadata != null)
				yield return metadata;
		}
	}

	private string GetMetadataPath(string id) {
		if (_isNonStandardPath)
			return Path.Combine(_registryPath, "metadata.json");
		else
            return Path.Combine(_registryPath, $"{id}.json");
    }
}