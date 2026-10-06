using Vestigium.Helpers.LogParser;

namespace Vestigium.Helpers.LogParser.Url;

public static class UrlReader
{
    public const long MaxBytes = 64L * 1024 * 1024;

    public static LogReadResult ReadFile(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("Path is required.", nameof(path));

        var info = new FileInfo(path);
        if (!info.Exists)
            throw new FileNotFoundException("Text file was not found.", path);
        if (info.Length > MaxBytes)
            throw new InvalidDataException("Text file is over 64 MB.");

        using var stream = File.OpenRead(path);
        return Read(stream);
    }

    public static LogReadResult Read(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);
        if (stream.CanSeek && stream.Length > MaxBytes)
            throw new InvalidDataException("Text file is over 64 MB.");

        using var reader = new StreamReader(stream, detectEncodingFromByteOrderMarks: true);
        _ = reader.ReadToEnd();
        return new LogReadResult(LogFormat.Url, 0, 0, []);
    }
}
