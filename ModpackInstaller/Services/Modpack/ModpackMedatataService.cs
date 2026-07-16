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
	private static readonly string DefaultRegistryPath;
	private readonly string _registryPath;
	private readonly bool _isStandardPath;

	static ModpackMedatataService() {
		DefaultRegistryPath = Path.Combine(AppVariables.InstallerRoot, "modpacks");
	}
	
	public static event Action? MetadataChanged;

	private static void RaiseMetadataChanged()
	{
		MetadataChanged?.Invoke();
	}

    /// <summary>
    /// Uses non discoverable custom metadata path
    /// </summary>
    public ModpackMedatataService(string registryPath) {
		_registryPath = registryPath;
		_isStandardPath = false;
		Directory.CreateDirectory(_registryPath);
	}

	/// <summary>
	/// Uses default AppVariables.InstallerRoot
	/// </summary>
	public ModpackMedatataService() {
		_registryPath = DefaultRegistryPath;
        _isStandardPath = true;
        Directory.CreateDirectory(_registryPath);
	}

	public void Create(ModpackMetadata metadata) {
		metadata.CreatedAt = DateTime.UtcNow;
		metadata.UpdatedAt = metadata.CreatedAt;

		Save(metadata);
	}

	public void Save(ModpackMetadata metadata) {

        metadata.UpdatedAt = DateTime.UtcNow;

		var path = GetMetadataPath(metadata.Id);
		var json = JsonSerializer.Serialize(metadata, AppVariables.DefaultJsonOptions);
		File.WriteAllText(path, json);
		RaiseMetadataChanged();
	}

    private bool TryLoad(Guid id, out ModpackMetadata? metadata) {
		metadata = null;
		var path = GetMetadataPath(id);

		if (!File.Exists(path))
			return false;

		try {
			var json = File.ReadAllText(path);
			
			metadata = JsonSerializer.Deserialize<ModpackMetadata>(json, AppVariables.DefaultJsonOptions);
			return metadata != null;
		}
		catch {
			return false;
		}
	}

    public ModpackMetadata Load() {
        if(_isStandardPath)
            throw new Exception("This is unavaliable when using a standard registry path.");

        TryLoad(Guid.Empty, out var metadata);

		return metadata ?? throw new Exception("Metadata does not exist");
    }
    
    public static ModpackMetadata? Load(string id) {
	    var path = GetDefaultMetadataPath(id);

	    try {
		    if (!File.Exists(path))
			    return null;
		    
		    var json = File.ReadAllText(path);
			
		    return JsonSerializer.Deserialize<ModpackMetadata>(json);
	    }
	    catch {
		    return null;
	    }
    }

    public static bool Exists(string id) {
        return File.Exists(GetDefaultMetadataPath(id));
	}

    public bool Exists() => _isStandardPath 
	    ? throw new Exception("This is unavaliable when using a standard registry path.") 
	    : File.Exists(GetMetadataPath(Guid.Empty));
    
    
	public static bool ExistsModpack(Guid modpackId) => 
		LoadAll().Any(x => x.ModpackId == modpackId);
	

    // 📌 Update parțial, safe
    public bool Update(Guid id, Action<ModpackMetadata> update) {
		if (!TryLoad(id, out var metadata) || metadata == null)
			return false;

        metadata.UpdatedAt = DateTime.UtcNow;

        update(metadata);
		Save(metadata);
		return true;
	}

	// 📌 Șterge metadata
	public bool Delete(Guid id, out DeleteError failReason) {
		failReason = DeleteError.None;
		var metadata = LoadAll().FirstOrDefault(x => x.Id == id);

		if (metadata is null) {
			failReason = DeleteError.MetadataNotFound;
			return false;
		}

		try {
			if (!string.IsNullOrWhiteSpace(metadata.InstallPath) &&
			    Directory.Exists(metadata.InstallPath))
				Directory.Delete(metadata.InstallPath, true);
		}
		catch {
			failReason = DeleteError.DirectoryDeleteFailed;
			return false;
		}

		try {
			var metadataPath = GetMetadataPath(id);

			if (File.Exists(metadataPath))
				File.Delete(metadataPath);
			
			RaiseMetadataChanged();
		}
		catch {
			failReason = DeleteError.MetadataDeleteFailed;
			return false;
		}

		return true;
	}
	
	public enum DeleteError
	{
		None,
		MetadataNotFound,
		DirectoryDeleteFailed,
		MetadataDeleteFailed
	}

	public static IEnumerable<ModpackMetadata> LoadAll() {
		return Directory.GetFiles(DefaultRegistryPath, "*.json")
			.Select(file => JsonSerializer.Deserialize<ModpackMetadata>(
				File.ReadAllText(file),
				AppVariables.DefaultJsonOptions
				))
			.OfType<ModpackMetadata>();
	}

	private string GetMetadataPath(Guid id) => !_isStandardPath
				? Path.Combine(_registryPath, "metadata.json")
				: Path.Combine(_registryPath, $"{id:N}.json");
	
	private static string GetDefaultMetadataPath(string id) =>
		Path.Combine(DefaultRegistryPath, $"{id}.json");
}