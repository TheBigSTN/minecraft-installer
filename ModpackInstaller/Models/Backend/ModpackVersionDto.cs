using System;
using ModpackInstaller.Models.DTOs;

namespace ModpackInstaller.Models.Backend;

public record ModpackVersionDto(
    Guid Id,
    string Semver,
    string VersionName,
    ModpackVersionStatus Status,
    string ModpackId,
    DateTimeOffset CreatedAt);