using System.Text;

namespace UpdateServer; 

public class SaltReader {
    /// <summary>
    ///     从指定的文件中读取 SHA-256 盐值。
    /// </summary>
    /// <param name="filePath">盐值文件路径。</param>
    /// <returns>盐值字符串。</returns>
    /// <exception cref="ArgumentNullException">filePath 为 null。</exception>
    /// <exception cref="FileNotFoundException">文件不存在。</exception>
    /// <exception cref="IOException">无法读取文件。</exception>
    /// <exception cref="ArgumentException">盐值不合法（长度不为 64 个字符）。</exception>
    public static string ReadSaltFromFile(string filePath) {
        if (filePath == null) throw new ArgumentNullException(nameof(filePath));

        if (!File.Exists(filePath)) throw new FileNotFoundException("File not found.", filePath);

        try {
            // 读取文件内容
            var salt = File.ReadAllText(filePath, Encoding.UTF8);

            // 裁剪掉意外输入的空格、回车
            salt = salt.Trim();

            // 检查盐值是否为合法的值（长度为 64 个字符）
            if (salt.Length != 64) throw new ArgumentException("Salt value is not valid (length must be 64 characters).");

            return salt;
        } catch (IOException ex) {
            throw new IOException("Error reading file.", ex);
        }
    }
}