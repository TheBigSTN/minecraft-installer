using System;
using System.IO;
using System.Security.Cryptography;

namespace ModpackInstaller.Services.FileSistem;

public static class FileHasher {
    public static string ComputeSha256(string filePath) {
        using var stream = File.OpenRead(filePath);
        using var sha = SHA256.Create();

        var hash = sha.ComputeHash(stream);

        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}