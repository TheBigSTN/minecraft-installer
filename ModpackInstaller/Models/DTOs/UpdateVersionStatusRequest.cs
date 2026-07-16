using System;

namespace ModpackInstaller.Models.DTOs;

public record UpdateVersionStatusRequest(
    Guid ModpackId,
    Guid VersionId, // Changed from string Semver to Guid VersionId
    ModpackVersionStatus NewStatus
);