using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using ModpackInstaller.Services.Modpack;

namespace ModpackInstaller.Models.FileSistem;

public class FileSelectionRules {
    private readonly HashSet<string> _managed = new (StringComparer.OrdinalIgnoreCase) {
        "mods"
    };
    private readonly HashSet<string> _required = new (StringComparer.OrdinalIgnoreCase) {
        "manifest.json"
    };

    private readonly List<Regex> _excludedRegexPatterns;
    private readonly HashSet<string> _excludedExactMatches;

    private readonly HashSet<string> _downloadableContent = [
        "tree.json"
    ];

    public FileSelectionRules(ModpackManifestStorage manifestStorage) {

        string[] excludedPatterns = [
            "TLauncherAdditional.json",
            "usercache.json",
            "usernamecache.json",
            "command_history.txt",
            "hs_err_pid*",
            "win_event*",
            ".sl_password",
            "emi.json",
            "xaero",
            "patch-manifest.json",
            "tree.json",
            "metadata.json"
        ];

        foreach (var modInfo in manifestStorage.InstalledMods) {
            if (modInfo.Source is ModSource.Local) {
                _required.Add(Path.Combine("mods", modInfo.Filename)
                    .Replace('\\', '/'));
                continue;
            }
            
            _downloadableContent.Add(Path.Combine("mods", modInfo.Filename)
                                            .Replace('\\', '/'));
        }
        
        _excludedRegexPatterns = [];
        _excludedExactMatches = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var pattern in excludedPatterns) {
            var normalizedPattern = pattern.Replace('\\', '/');
            if (normalizedPattern.Contains('*')) {
                // Convert wildcard pattern to regex
                var regexPattern = "^" + Regex.Escape(normalizedPattern).Replace("\\*", ".*") + "$";
                _excludedRegexPatterns.Add(new Regex(regexPattern, RegexOptions.IgnoreCase | RegexOptions.Compiled));
            } else {
                _excludedExactMatches.Add(normalizedPattern);
            }
        }
    }

    public FileSelectionState GetState(string path) {
        var normalizedPath = path.Replace('\\', '/');

        if (_required.Contains(normalizedPath))
            return FileSelectionState.Required;

        if (_managed.Contains(normalizedPath))
            return FileSelectionState.Managed;

        return _downloadableContent.Contains(normalizedPath) 
            ? FileSelectionState.Downloadable 
            : FileSelectionState.Normal;
    }

    public bool IsDisabledByDefault(string path) {
        var normalizedPath = path.Replace('\\', '/');

        // Check for exact matches first
        if (_excludedExactMatches.Contains(normalizedPath)) {
            return true;
        }

        if (_downloadableContent.Contains(normalizedPath))
            return true;

        // Check against pre-compiled regex patterns
        return _excludedRegexPatterns
            .Any(regex => regex.IsMatch(normalizedPath));
    }
}