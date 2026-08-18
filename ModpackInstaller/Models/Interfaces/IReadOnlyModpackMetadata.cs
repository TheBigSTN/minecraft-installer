using System;
using ModpackInstaller.Services;

namespace ModpackInstaller.Models.Interfaces;

public interface IReadOnlyModpackMetadata : IModrinthSearchInfo {
    Guid Id { get; }
    Guid? ModpackId { get; }
    string Name { get; }
    Guid VersionId { get; }
    string VersionSemver { get; }
    string? SharingCode { get; }
    string? ModpackPassword { get; }
    ModpackSource Source { get; }
    new string GameVersion { get; }
    new ModLoaderType Loader { get; }
    string LoaderVersion { get; }
    string Author { get; }
    string? Description { get; }
    string InstallPath { get; }
    DateTimeOffset CreatedAt { get; }
    DateTimeOffset UpdatedAt { get; }
    bool IsServerInstall { get; }
    string Icon { get; }
}