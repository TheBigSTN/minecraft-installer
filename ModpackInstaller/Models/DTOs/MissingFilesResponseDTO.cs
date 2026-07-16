using System.Collections.Generic;

namespace ModpackInstaller.Models.DTOs;

public record MissingFilesResponseDto(
    List<ModpackFileDto> MissingFiles
);