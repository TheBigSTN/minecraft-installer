using System;

namespace ModpackInstaller.Models.Backend;

public record ModpackDto(
    Guid Id,
    string Name,
    UserDto Owner,
    string GameVersion,
    ModLoaderType Loader,
    string LoaderVersion,
    ModpackVersionDto LatestVersion,
    string ShareCode,
    string? Password);