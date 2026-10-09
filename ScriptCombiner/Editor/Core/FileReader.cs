using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public static class FileReader
{
    public static async Task<string> ReadFileWithAutoEncodingAsync(string path)
    {
        byte[] fileBytes = await ReadAllBytesAsync(path);
        if (fileBytes.Length == 0) return string.Empty;

        if (fileBytes.Length >= 3 && fileBytes[0] == 0xEF && fileBytes[1] == 0xBB && fileBytes[2] == 0xBF)
            return Encoding.UTF8.GetString(fileBytes, 3, fileBytes.Length - 3);

        if (fileBytes.Length >= 2 && fileBytes[0] == 0xFF && fileBytes[1] == 0xFE)
            return Encoding.Unicode.GetString(fileBytes, 2, fileBytes.Length - 2);

        if (fileBytes.Length >= 2 && fileBytes[0] == 0xFE && fileBytes[1] == 0xFF)
            return Encoding.BigEndianUnicode.GetString(fileBytes, 2, fileBytes.Length - 2);

        Encoding[] encodingsToTry = { Encoding.UTF8, Encoding.GetEncoding(1251), Encoding.Default };
        foreach (var encoding in encodingsToTry)
        {
            string content = encoding.GetString(fileBytes);
            if (ContainsMeaningfulContent(content)) return content;
        }
        return Encoding.UTF8.GetString(fileBytes);
    }

    private static async Task<byte[]> ReadAllBytesAsync(string path)
    {
        using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, useAsync: true))
        {
            byte[] buffer = new byte[stream.Length];
            int bytesRead = 0;
            while (bytesRead < stream.Length)
            {
                bytesRead += await stream.ReadAsync(buffer, bytesRead, (int)stream.Length - bytesRead);
            }
            return buffer;
        }
    }

    private static bool ContainsMeaningfulContent(string content)
    {
        return content.Any(c => c >= 'A' && c <= 'z') || content.Any(c => c >= 'À' && c <= 'ÿ');
    }
}