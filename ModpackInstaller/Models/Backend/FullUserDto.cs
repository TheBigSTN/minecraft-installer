using System;

namespace ModpackInstaller.Models.Backend;

public record FullUserDto(
    Guid Id, 
    string Token,
    string Username,
    DateTimeOffset CreatedAt);