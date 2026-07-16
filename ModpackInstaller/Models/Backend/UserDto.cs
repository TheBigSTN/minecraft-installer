using System;

namespace ModpackInstaller.Models.Backend;

public record UserDto(
    Guid Id, 
    string Username,
    DateTimeOffset CreatedAt);