using System.Diagnostics;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Web;
namespace UpdateServer;

internal static class Server{
    private static ServerConfig _serverConfig = new();
    private static string? _targetFolderPath;
    private static int _serverPort;
    private const int MaxPathSize = 4096;
    private static string? _salt;

    /// <summary>
    ///     启动服务器。
    /// </summary>
    /// <returns>Task, 服务器启动操作。</returns>
    public static async Task Main() {
        _serverConfig = XmlConfigHandler.ParseServerConfig("ServerConfig.xml");
        _targetFolderPath = _serverConfig.TargetFolderPath;
        _serverPort = _serverConfig.ServerPort;
        _salt = _serverConfig.Salt;
        using var cc = new ConsoleLogHandler("UpdateRequest.log");
        var cts = new CancellationTokenSource();
        var fileMetaDataList = ScanJarFolder();
        var listener = new HttpListener();
        listener.Prefixes.Add($"http://*:{_serverPort}/");
#pragma warning disable CS4014
        if (_serverConfig.HeartBeatUrl != null) new Heartbeats(_serverConfig.HeartBeatUrl).StartCheckingAsync(cts.Token);
#pragma warning restore CS4014
        listener.Start();
        Console.WriteLine("Server started on port " + _serverPort);
        var tasks = new List<Task>();
        var activeConnections = 0;
        const int maxConnections = 100; // adjust base on network performance.

        try {
            while (true) {
                if (activeConnections >= maxConnections) {
                    await Task.Delay(1000); // wait for a while before checking again
                    continue;
                }

                var context = await listener.GetContextAsync();

                var task = HandleClientAsync(context, fileMetaDataList);
                tasks.Add(task);
                activeConnections++;

                var completedTask = await Task.WhenAny(tasks);
                tasks.Remove(completedTask);
                activeConnections--;
            }
        } catch (Exception e) {
            Console.WriteLine(e);
            throw;
        }
    }

    /// <summary>
    ///     处理客户端更新请求。
    /// </summary>
    /// <param name="context">HTTP 监听器上下文。</param>
    /// <param name="fileMetaDataList">文件元数据列表。</param>
    /// <returns>Task, 表示处理客户端更新请求的操作。</returns>
    private static async Task HandleClientAsync(HttpListenerContext context, FileMetaDataList fileMetaDataList) {
        var httpListenerContext = context;
        var clientIp = httpListenerContext.Request.RemoteEndPoint.Address.ToString();
        try {
            if (httpListenerContext.Request.Url != null) {
                var queryString = HttpUtility.ParseQueryString(httpListenerContext.Request.Url.Query);
                var authToken = queryString["authToken"];

                var stream = httpListenerContext.Response.OutputStream;
                var buffer = new byte[MaxPathSize];
                var bytesRead = await httpListenerContext.Request.InputStream.ReadAsync(buffer, 0, buffer.Length);
                if (bytesRead > MaxPathSize) Array.Resize(ref buffer, MaxPathSize);

                if (!AuthenticateClientAsync(authToken)) {
                    httpListenerContext.Response.StatusCode = 403; // Unauthorized
                    Console.WriteLine($"{clientIp} -- [{DateTime.Now}]: \"{httpListenerContext.Request.RawUrl} \" -- {httpListenerContext.Response.StatusCode} -- \"{httpListenerContext.Request.UserAgent}\" {httpListenerContext.Request.ProtocolVersion} {httpListenerContext.Request.HttpMethod}");
                    httpListenerContext.Response.Close();
                    return;
                }

                Console.WriteLine($"{clientIp} -- [{DateTime.Now}]: \"{httpListenerContext.Request.RawUrl} \" -- {httpListenerContext.Response.StatusCode} -- \"{httpListenerContext.Request.UserAgent}\" {httpListenerContext.Request.ProtocolVersion} {httpListenerContext.Request.HttpMethod}");
                httpListenerContext.Response.ContentType = "application/json";
                await JsonSerializer.SerializeAsync(stream, fileMetaDataList);
            }

            httpListenerContext.Response.Close();
        } catch (Exception ex) {
            Console.WriteLine($"An error occurred: {ex.Message}");
            httpListenerContext.Response.StatusCode = 500; // Internal Server Error
        }
    }
    
    /// <summary>
    ///     验证客户端身份。
    /// </summary>
    /// <param name="authToken">客户端身份令牌。</param>
    /// <returns>如果客户端身份验证通过，则返回 true；否则，返回 false。</returns>
    private static bool AuthenticateClientAsync(string? authToken) {
        var currentTime = DateTime.UtcNow;
        var tolerance = TimeSpan.FromMinutes(5);
        var startTime = currentTime - tolerance;
        var endTime = currentTime + tolerance;

        return Enumerable.Range(0, (int)tolerance.TotalMinutes + 1).Select(i => startTime.AddMinutes(i)).Select(time => ComputeHash(time.ToString("yyyy.MM.dd.HH.mm") + _salt)).Any(dynamicToken => authToken == dynamicToken) || Enumerable.Range(0, (int)tolerance.TotalMinutes + 1).Reverse().Select(i => endTime.AddMinutes(-i)).Select(time => ComputeHash(time.ToString("yyyy.MM.dd.HH.mm") + _salt)).Any(dynamicToken => authToken == dynamicToken);
    }

    private static string ComputeHash(string input) {
#pragma warning disable SYSLIB0021
        using var sha256 = new SHA256Managed();
#pragma warning restore SYSLIB0021
        var bytes = Encoding.UTF8.GetBytes(input);
        var hashBytes = sha256.ComputeHash(bytes);
        var hashString = BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
        return hashString;
    }

    private static FileMetaDataList ScanJarFolder() {
        var fileMetaDataList = new FileMetaDataList();
        var jarFilesIncrementPath = Path.Combine(_targetFolderPath ?? throw new InvalidOperationException(), "increment");
        var jarFilesDecrementPath = Path.Combine(_targetFolderPath, "decrement");
        if (!Directory.Exists(jarFilesIncrementPath)) Directory.CreateDirectory(jarFilesIncrementPath);
        if (!Directory.Exists(jarFilesDecrementPath)) Directory.CreateDirectory(jarFilesDecrementPath);
        var jarFilesIncrement = Directory.GetFiles(jarFilesIncrementPath, "*.jar");
        var jarFilesDecrement = Directory.GetFiles(jarFilesDecrementPath, "*.jar");
        foreach (var jarFileIncrement in jarFilesIncrement) {
            var fileInfo = new FileInfo(jarFileIncrement);
            var fileHash = ComputeFileHash(jarFileIncrement);

            var fileMetaData = new FileMetaData(
                fileInfo.Name,
                fileInfo.Length,
                FileVersionInfo.GetVersionInfo(jarFileIncrement).FileVersion ?? string.Empty,
                fileHash,
                "Add");

            fileMetaDataList.AddFile(fileMetaData);
        }

        foreach (var jarFileDecrement in jarFilesDecrement) {
            var fileInfo = new FileInfo(jarFileDecrement);
            var fileHash = ComputeFileHash(jarFileDecrement);

            var fileMetaData = new FileMetaData(
                fileInfo.Name,
                fileInfo.Length,
                FileVersionInfo.GetVersionInfo(jarFileDecrement).FileVersion ?? string.Empty,
                fileHash,
                "Del");

            fileMetaDataList.AddFile(fileMetaData);
        }

        return fileMetaDataList;
    }

    private static string ComputeFileHash(string filePath) {
        using var fileStream = File.OpenRead(filePath);
        using var sha256 = SHA256.Create();
        var hash = sha256.ComputeHash(fileStream);
        return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
    }
}