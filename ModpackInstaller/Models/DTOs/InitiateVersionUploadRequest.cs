using System;

namespace ModpackInstaller.Models.DTOs;

public record InitiateVersionUploadRequest(
    Guid ModpackId,
    string Semver,
    string VersionName
);