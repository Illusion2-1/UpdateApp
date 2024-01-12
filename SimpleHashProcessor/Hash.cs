using System.Security.Cryptography;
using System.Text;

namespace SimpleHashProcessor;

public static class Hash {
    public static string GenerateDynamicToken(string salt) {
        var currentTime = DateTime.UtcNow;
        var hashedTime = ComputeHash(currentTime.ToString("yyyy.MM.dd.HH.mm") + salt);
        return hashedTime;
    }

    private static string ComputeHash(string input) {
        using var sha256 = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(input);
        var hashBytes = sha256.ComputeHash(bytes);
        var hashString = BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
        return hashString;
    }
}