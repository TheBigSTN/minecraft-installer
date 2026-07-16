using System;

namespace ModpackInstaller.Models;

public record CreateModpackRequest(
    Guid Id,
    string Name,
    string Description,
    string GameVersion,
    string Loader,
    string LoaderVersion,
    string SharingCode,
    bool Public
);