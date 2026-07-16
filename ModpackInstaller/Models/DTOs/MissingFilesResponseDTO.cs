using System.Collections.Generic;

namespace ModpackInstaller.Models.DTOs;

public record MissingFilesResponseDTO(
    List<ModpackFileDto> MissingFiles
);