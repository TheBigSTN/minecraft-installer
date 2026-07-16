using System.Collections.Generic;

namespace ModpackInstaller.Models.DTOs;

public record ModpackTreeDto(
    List<ModpackFileDto> Files
);