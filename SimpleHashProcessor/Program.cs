using UpdateServer;
namespace SimpleHashProcessor;

internal static class SimpleHashProcessor {
    public static void Main() {
        var salt = SaltReader.ReadSaltFromFile(Path.Combine(Directory.GetCurrentDirectory(), "sha256"));
        var currentTime = DateTime.UtcNow;
        Console.Write($"时间{currentTime:yyyy.MM.dd.HH:mm}的动态hash为{Hash.GenerateDynamicToken(salt)}");
    }
}