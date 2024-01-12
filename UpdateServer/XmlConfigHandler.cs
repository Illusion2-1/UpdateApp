using System.Data;
using System.Xml;
using System.Xml.Serialization;

namespace UpdateServer;

/// <summary>
///     XML 配置文件解析类。
/// </summary>
public static class XmlConfigHandler {
    /// <summary>
    ///     解析客户端配置文件。
    /// </summary>
    /// <param name="xmlFilePath">配置文件路径。</param>
    /// <returns>客户端配置文件对象。</returns>
    /// <exception cref="ArgumentNullException">xmlFilePath 为 null。</exception>
    /// <exception cref="FileNotFoundException">文件不存在。</exception>
    /// <exception cref="IOException">无法读取文件。</exception>
    /// <exception cref="NullReferenceException">配置文件为空。</exception>
    public static ClientConfig ParseClientConfig(string xmlFilePath) {
        var serializer = new XmlSerializer(typeof(ClientConfig));
        using var reader = XmlReader.Create(xmlFilePath);
        return (ClientConfig?)serializer.Deserialize(reader) ?? throw new NullReferenceException("config.xml is null");
    }

    /// <summary>
    ///     解析服务器配置文件。
    /// </summary>
    /// <param name="xmlFilePath">配置文件路径。</param>
    /// <returns>服务器配置文件对象。</returns>
    /// <exception cref="ArgumentNullException">xmlFilePath 为 null。</exception>
    /// <exception cref="FileNotFoundException">文件不存在。</exception>
    /// <exception cref="IOException">无法读取文件。</exception>
    /// <exception cref="NullReferenceException">配置文件为空。</exception>
    public static ServerConfig ParseServerConfig(string xmlFilePath) {
        var serializer = new XmlSerializer(typeof(ServerConfig));
        using var reader = XmlReader.Create(xmlFilePath);
        return (ServerConfig?)serializer.Deserialize(reader) ?? throw new NullReferenceException("config.xml is null");
    }
}

/// <summary>
///     客户端配置文件类。
/// </summary>
[XmlRoot("Client")]
public class ClientConfig {
    /// <summary>
    ///     资源地址。
    /// </summary>
    [XmlElement("ResourceUri")]
    public string? ResourceUri { get; set; }

    /// <summary>
    ///     基础路径。
    /// </summary>
    [XmlElement("BasePath")]
    public string? BasePath { get; set; }

    /// <summary>
    ///     引用路径。
    /// </summary>
    [XmlElement("RefPath")]
    public string? RefPath { get; set; }

    /// <summary>
    ///     盐值。
    /// </summary>
    [XmlElement("Salt")]
    public string? Salt { get; set; }

    /// <summary>
    ///     JSON 数据地址。
    /// </summary>
    [XmlElement("JsonDataAddress")]
    public string? JsonDataAddress { get; set; }

    /// <summary>
    ///     JSON 数据地址端口。
    /// </summary>
    [XmlElement("JsonDataAddressPort")]
    public int JsonDataAddressPort { get; set; }
}

/// <summary>
///     服务器配置文件类。
/// </summary>
[XmlRoot("Server")]
public class ServerConfig {
    /// <summary>
    ///     目标文件夹路径。
    /// </summary>
    [XmlElement("TargetFolderPath")]
    public string? TargetFolderPath { get; set; }

    /// <summary>
    ///     服务器端口。
    /// </summary>
    [XmlElement("ServerPort")]
    public int ServerPort { get; set; }

    /// <summary>
    ///     盐值。
    /// </summary>
    [XmlElement("Salt")]
    public string? Salt { get; set; }

    /// <summary>
    ///     心跳 URL。
    /// </summary>
    [XmlElement("HeartBeatUrl")]
    public string? HeartBeatUrl { get; set; }
}
