namespace ModpackInstaller.Models.DTOs;

public record ModpackFileDto(
    string FilePath,
    string Sha256,
    long FileSize
);