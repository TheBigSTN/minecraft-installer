using System;
using System.Security.Cryptography;
using System.IO;

namespace ModpackInstaller.Services;

public static class HashUtils {
    public static string ComputeSHA1( string filePath ) {
        using var stream = File.OpenRead(filePath);
        using var sha1 = SHA1.Create();
        var hash = sha1.ComputeHash(stream);

        return BitConverter.ToString(hash)
            .Replace("-", "")
            .ToLowerInvariant();
    }
}