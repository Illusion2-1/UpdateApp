using System.Diagnostics;
using UpdateServer;

namespace UpdateApp;

internal static class Updater {
    private static readonly object ConsoleLock = new();
    public static ClientConfig? ClientConfig;
    private static Uri? _baseUri;
    private static string? _basePath;
    private static string? _refPath;

    public static async Task Main() {
        ClientConfig = XmlConfigHandler.ParseClientConfig("config.xml");
        Debug.Assert(ClientConfig != null, nameof(ClientConfig) + " != null");
        Debug.Assert(ClientConfig.ResourceUri != null, "clientConfig.ResourceUri != null");
        Debug.Assert(ClientConfig.BasePath != null, "clientConfig.BasePath != null");
        Debug.Assert(ClientConfig.RefPath != null, "clientConfig.RefPath != null");
        _baseUri = new Uri(ClientConfig.ResourceUri);
        _basePath = ClientConfig.BasePath;
        _refPath = ClientConfig.RefPath;
        Path.Combine(_basePath, _basePath);
        if (IsRunningFromCompressedFolder())
            throw new Exception("Unexpected Behavior: Program is not extracted.");

        using var cc = new ConsoleLogHandler("Logging.tmp");
        Console.ForegroundColor = ConsoleColor.Cyan;
        if (!Directory.Exists(_basePath)) {
            Directory.CreateDirectory(_basePath);
            Directory.CreateDirectory(Path.Combine(_basePath, _basePath));
        }

        ConsoleHelper.DisableConsoleWrapping(); //并没有Disable

        try {
            Console.WriteLine("Initialized.");
            Console.WriteLine("Trying to fetch any available update.");

            var updateListTask = SerializationHandler.GetFilesAsync("Add");
            var deleteListTask = SerializationHandler.GetFilesAsync("Del");

            await Task.WhenAll(updateListTask, deleteListTask);

            var updateList = updateListTask.Result;
            var deleteList = deleteListTask.Result;

            var line = 5;

            if (updateList.Any()) {
                Console.WriteLine("Files available, fetching.");
                List<Task> tasks = new();

                foreach (var fileToDownload in updateList) {
                    var uri = new Uri(_baseUri, fileToDownload.FileName);
                    var downloadTask = DownloadAsync(uri, Path.Combine(_basePath, _refPath), fileToDownload.FileName, line++);
                    tasks.Add(downloadTask);
                }

                await Task.WhenAll(tasks);
                Console.WriteLine("\n\nCompleted.");
            } else {
                Console.WriteLine("No update available.");
            }
            if (deleteList.Any())
                foreach (var fileToDelete in deleteList) {
                    var pathToFile = Path.Combine(_basePath, _refPath, fileToDelete.FileName);
                    File.Delete(pathToFile);
                    SafeUpdateConsole(0, line++, $"Deleted {fileToDelete.FileName}");
                }
            Console.ReadKey();
        } catch (Exception ex) {
            ExceptionHandler.PrintException(ex.ToString());
            Console.WriteLine($"{ex.Message}");
        }
    }
    /// <summary>
    ///     异步下载文件。
    /// </summary>
    /// <param name="url">文件的 URL。</param>
    /// <param name="path">文件的保存路径。</param>
    /// <param name="filename">文件的名称。</param>
    /// <param name="lineCount">要输出进度信息的控制台行号。</param>
    /// <returns>下载任务。</returns>
    private static async Task DownloadAsync(Uri url, string path, string filename, int lineCount) {
        using var client = new HttpClient();
        using var response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
        response.EnsureSuccessStatusCode();

        var contentLength = response.Content.Headers.ContentLength ?? -1;
        var stopwatch = Stopwatch.StartNew();
        var fullPath = Path.Combine(path, filename);

        await using var fileStream = new FileStream(fullPath, FileMode.OpenOrCreate, FileAccess.Write, FileShare.Write);
        await using var contentStream = await response.Content.ReadAsStreamAsync();
        var buffer = new byte[1048576];
        long downloaded = 0;
        int bytesRead;

        while ((bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length)) > 0) {
            await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead));
            downloaded += bytesRead;

            var progress = (int)Math.Round((double)downloaded / contentLength * 100);
            var speed = downloaded / stopwatch.Elapsed.TotalSeconds;

            var progressLength = (int)Math.Round((double)progress / 100 * 40);
            var progressBar = new string('=', progressLength) + new string('-', 40 - progressLength);
            var progressText = $"Downloading... {progress}% [{progressBar}]  {filename} - {FormatSize(speed)}    ";

            SafeUpdateConsole(0, lineCount, progressText);
        }
    }

    /// <summary>
    ///     更新控制台上的文本。
    /// </summary>
    /// <param name="cursorLeft">光标的列位置。</param>
    /// <param name="cursorTop">光标的行位置。</param>
    /// <param name="text">要输出的文本。</param>
    private static void SafeUpdateConsole(int cursorLeft, int cursorTop, string text) {
        lock (ConsoleLock) {
            Console.SetCursorPosition(cursorLeft, cursorTop);
            Console.Write(text);
        }
    }

    /// <summary>
    ///     格式化文件大小。
    /// </summary>
    /// <param name="speed">文件大小（字节/秒）。</param>
    /// <returns>格式化后的文件大小字符串。</returns>
    private static string FormatSize(double speed) {
        return speed switch {
            < 1024 => $"{speed:F2} B/s",
            < 1024 * 1024 => $"{speed / 1024:F2} KiB/s",
            _ => speed < 1024 * 1024 * 1024
                ? $"{speed / (1024 * 1024):F2} MiB/s"
                : $"{speed / (1024 * 1024 * 1024):F2} GiB/s"
        };
    }
    /// <summary>
    ///     使用非常简单的手段检查程序是否从压缩文件夹中运行。
    /// </summary>
    /// <returns>如果程序从压缩文件夹中运行，则返回 true；否则返回 false。</returns>
    private static bool IsRunningFromCompressedFolder() {
        if (Environment.OSVersion.Platform != PlatformID.Win32NT)
            return false; // Skip checking for non-Windows systems

        var tempFolderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Temp");
        var currentDirectory = Directory.GetCurrentDirectory();

        return currentDirectory.Contains(tempFolderPath);
    }
}