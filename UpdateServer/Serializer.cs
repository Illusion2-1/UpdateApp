namespace UpdateServer;

[Serializable]
public class FileMetaData {
    public FileMetaData(string fileName, long fileSize, string fileVersion, string fileHash, string fileAction) {
        FileName = fileName;
        FileSize = fileSize;
        FileVersion = fileVersion;
        FileHash = fileHash;
        FileAction = fileAction;
    }

    public string FileName { get; set; }
    public long FileSize { get; set; }
    public string FileVersion { get; set; }
    public string FileHash { get; set; }
    public string FileAction { get; set; }
}

[Serializable]
public class FileMetaDataList {
    public FileMetaDataList() {
        Files = new List<FileMetaData>();
    }

    public List<FileMetaData> Files { get; set; }

    public void AddFile(FileMetaData file) {
        Files.Add(file);
    }
}