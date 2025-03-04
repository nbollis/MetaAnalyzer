using System.IO.Compression;
namespace ResultAnalyzerUtil;

public static class GzipHandler
{
    /// <summary>
    /// Decompresses a GZ file and returns its contents as a string.
    /// </summary>
    public static string DecompressGzipToString(string gzipFilePath)
    {
        using FileStream fileStream = new(gzipFilePath, FileMode.Open, FileAccess.Read);
        using GZipStream gzipStream = new(fileStream, CompressionMode.Decompress);
        using StreamReader reader = new(gzipStream);
        return reader.ReadToEnd();
    }

    /// <summary>
    /// Decompresses a GZ file and saves it to an output file, retaining the original extension.
    /// </summary>
    public static string DecompressGzipToFile(string gzipFilePath)
    {
        if (!File.Exists(gzipFilePath))
            throw new FileNotFoundException("The specified file does not exist.", gzipFilePath);

        // Get the original file name by removing ".gz"
        string outputFilePath = GetOriginalFileName(gzipFilePath);

        using FileStream compressedStream = new(gzipFilePath, FileMode.Open, FileAccess.Read);
        using FileStream outputStream = new(outputFilePath, FileMode.Create, FileAccess.Write);
        using GZipStream gzipStream = new(compressedStream, CompressionMode.Decompress);
        gzipStream.CopyTo(outputStream);

        return outputFilePath;
    }

    /// <summary>
    /// Decompresses GZ data from a byte array and returns the decompressed bytes.
    /// </summary>
    public static byte[] DecompressGzipFromBytes(byte[] compressedData)
    {
        using MemoryStream inputStream = new(compressedData);
        using MemoryStream outputStream = new();
        using GZipStream gzipStream = new(inputStream, CompressionMode.Decompress);
        gzipStream.CopyTo(outputStream);
        return outputStream.ToArray();
    }

    /// <summary>
    /// Helper method to generate the original filename by removing the .gz extension.
    /// </summary>
    private static string GetOriginalFileName(string gzipFilePath)
    {
        if (gzipFilePath.EndsWith(".gz", StringComparison.OrdinalIgnoreCase))
        {
            return gzipFilePath[..^3]; // Remove ".gz"
        }
        return gzipFilePath + ".decompressed"; // Fallback if the file does not have .gz
    }
}
