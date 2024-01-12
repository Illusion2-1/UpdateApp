using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using UpdateServer;
namespace UpdateApp;

/// <summary>
///     内部静态类，用于处理序列化。
/// </summary>
internal static class SerializationHandler {
    private static string? _serverAddress;
    private static int _serverPort;
    private static FileMetaDataList? _cachedServerFileMetaDataList;

    /// <summary>
    ///     生成动态令牌。
    /// </summary>
    /// <returns>动态令牌。</returns>
    private static string GenerateDynamicToken() {
        Debug.Assert(Updater.ClientConfig != null, "Updater.ClientConfig != null");
        var salt = Updater.ClientConfig.Salt;
        var currentTime = DateTime.UtcNow;
        var hashedTime = ComputeHash(currentTime.ToString("yyyy.MM.dd.HH.mm") + salt);
        return hashedTime;
    }

    /// <summary>
    ///     获取文件列表。
    /// </summary>
    /// <param name="action">要执行的操作（"Add" 或 "Del"）。</param>
    /// <returns>文件列表。</returns>
    public static async Task<List<FileMetaData>> GetFilesAsync(string action) {
        var jarFolderPath = GetJarFolderPath();
        var localFileMetaDataList = ScanLocalJarFolder(jarFolderPath);
        _cachedServerFileMetaDataList ??= await RequestServerFileMetaDataListAsync();
        if (action == "Add") {
            var filesToUpdate = GetIncrementLists(localFileMetaDataList, _cachedServerFileMetaDataList);
            return filesToUpdate;
        }

        if (action == "Del") {
            var filesToDelete = GetDecrementLists(localFileMetaDataList, _cachedServerFileMetaDataList);
            return filesToDelete;
        }

        throw new NotImplementedException();
    }
    
    /// <summary>
    ///     获取 Jar 文件夹的路径。
    /// </summary>
    /// <returns>Jar 文件夹的路径。</returns>
    private static string GetJarFolderPath() {
        Debug.Assert(Updater.ClientConfig != null, "Updater.ClientConfig != null");
        Debug.Assert(Updater.ClientConfig.BasePath != null, "Updater.ClientConfig.BasePath != null");
        Debug.Assert(Updater.ClientConfig.RefPath != null, "Updater.ClientConfig.RefPath != null");
        var path = Path.Combine(Updater.ClientConfig.BasePath, Updater.ClientConfig.RefPath);
        if (!Directory.Exists(path)) Directory.CreateDirectory(path);

        return path;
    }

    /// <summary>
    ///     扫描本地 Jar 文件夹。
    /// </summary>
    /// <param name="jarFolderPath">Jar 文件夹的路径。</param>
    /// <returns>文件元数据列表。</returns>
    private static FileMetaDataList ScanLocalJarFolder(string jarFolderPath) {
        var fileMetaDataList = new FileMetaDataList();

        var jarFiles = Directory.GetFiles(jarFolderPath, "*.jar");

        foreach (var jarFile in jarFiles) {
            var fileInfo = new FileInfo(jarFile);
            var fileHash = ComputeFileHash(jarFile);

            var fileMetaData = new FileMetaData(
                fileInfo.Name,
                fileInfo.Length,
                FileVersionInfo.GetVersionInfo(jarFile).FileVersion ?? string.Empty,
                fileHash,
                "null");

            fileMetaDataList.AddFile(fileMetaData);
        }

        return fileMetaDataList;
    }

    
    private static async Task<FileMetaDataList?> RequestServerFileMetaDataListAsync() {
        Debug.Assert(Updater.ClientConfig != null, "Updater.ClientConfig != null");
        _serverAddress = Updater.ClientConfig.JsonDataAddress;
        _serverPort = Updater.ClientConfig.JsonDataAddressPort;
        var client = new HttpClient();
        var authToken = GenerateDynamicToken();
        var request = new HttpRequestMessage(HttpMethod.Get, $"http://{_serverAddress}:{_serverPort}/?authToken={authToken}");
        var response = await client.SendAsync(request);
        response.EnsureSuccessStatusCode();
        var serverFileMetaDataList = await JsonSerializer.DeserializeAsync<FileMetaDataList>(await response.Content.ReadAsStreamAsync());

        return serverFileMetaDataList;
    }

    /// <summary>
    ///     获取要更新的文件列表。
    /// </summary>
    /// <param name="localFileMetaDataList">本地文件元数据列表。</param>
    /// <param name="serverFileMetaDataList">服务器文件元数据列表。</param>
    /// <returns>要更新的文件列表。</returns>
    private static List<FileMetaData> GetIncrementLists(FileMetaDataList localFileMetaDataList, FileMetaDataList? serverFileMetaDataList) {
        var filesToUpdate = new List<FileMetaData>();
        if (serverFileMetaDataList == null) return filesToUpdate;
        var localFilesByHash = localFileMetaDataList.Files.ToDictionary(file => file.FileHash, file => file);
        foreach (var serverFile in serverFileMetaDataList.Files) {
            if (serverFile.FileAction != "Add") continue;
            if (!localFilesByHash.TryGetValue(serverFile.FileHash, out var matchingLocalFile)) {
                filesToUpdate.Add(serverFile);
            } else {
                if (matchingLocalFile.FileName != serverFile.FileName) {
                    Console.WriteLine($"Skipping {serverFile.FileName}:{serverFile.FileHash}");
                    Console.WriteLine("for same hash value.");
                }
            }
        }
        return filesToUpdate;
    }


    private static List<FileMetaData> GetDecrementLists(FileMetaDataList localFileMetaDataList, FileMetaDataList? serverFileDataList) {
        var filesToDelete = new List<FileMetaData>();
        if (serverFileDataList == null) return filesToDelete;
        foreach (var serverFile in serverFileDataList.Files)
        foreach (var localFile in localFileMetaDataList.Files)
            if (serverFile.FileName == localFile.FileName && serverFile.FileAction == "Del")
                filesToDelete.Add(serverFile);

        return filesToDelete;
    }

    private static string ComputeFileHash(string filePath) {
        using var fileStream = File.OpenRead(filePath);
        using var sha256 = SHA256.Create();
        var hash = sha256.ComputeHash(fileStream);
        return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
    }

    private static string ComputeHash(string input) {
        using var sha256 = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(input);
        var hashBytes = sha256.ComputeHash(bytes);
        var hashString = BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
        return hashString;
    }
}