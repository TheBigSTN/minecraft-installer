namespace ModpackInstaller.Models.FileSistem;

public sealed class TreeNodeInfo
{
    public required string FullPath { get; init; }

    public required string RelativePath { get; init; }

    public required string Name { get; init; }

    public required bool IsDirectory { get; init; }
}