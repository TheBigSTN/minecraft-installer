using System.Collections.Generic;
using System.IO;

namespace ModpackInstaller.Services.FileSistem;

public static class ConfigFileParser {
    public static LinkedList<ConfigLineBase> ParseFile(string path) {
        var extension = Path.GetExtension(path);

        if (extension != "toml") return [];
        
        var lines = File.ReadAllLines(path);
        var parsedLines = new LinkedList<ConfigLineBase>();

        foreach (var line in lines) {
            if (line.StartsWith('#')) {
                parsedLines.AddLast(new ConfigLineComment(line, parsedLines.Count));
            }
        }
        
        return parsedLines;
    }
}

public class ConfigLineBase(string rawLine, int lineNumber) {
    public int LineNumber = lineNumber;
    public string RawLine = rawLine;
}

public class ConfigLineComment : ConfigLineBase {
    public ConfigLineComment(string rawLine, int lineNumber) : base(rawLine, lineNumber) {
        RawLine = rawLine;
        LineNumber = lineNumber;
    }
}

public class ConfigLineValue<T> : ConfigLineBase {
    public T Value;
    public string FieldName;
    public string Template;

    public ConfigLineValue(string rawLine, int lineNumber) : base(rawLine, lineNumber) {
        RawLine = rawLine;
        LineNumber = lineNumber;
        Value = default!;
        FieldName = string.Empty;
        Template = string.Empty;
    }
}