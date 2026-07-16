namespace ModpackInstaller.Models.DTOs;

public record ModpackFileDto(
    string FilePath,
    string Sha256, // Changed from FileHash to Sha256
    long FileSize
);