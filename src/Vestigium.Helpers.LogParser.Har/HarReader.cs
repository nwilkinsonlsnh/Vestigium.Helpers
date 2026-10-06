using System.Text.Json;
using Vestigium.Helpers.LogParser;

namespace Vestigium.Helpers.LogParser.Har;

public static class HarReader
{
    public const long MaxBytes = 64L * 1024 * 1024;

    public static LogReadResult ReadFile(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("Path is required.", nameof(path));

        var info = new FileInfo(path);
        if (!info.Exists)
            throw new FileNotFoundException("HAR file was not found.", path);
        if (info.Length > MaxBytes)
            throw new InvalidDataException("HAR file is over 64 MB.");

        using var stream = File.OpenRead(path);
        return Read(stream);
    }

    public static LogReadResult Read(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);
        if (stream.CanSeek && stream.Length > MaxBytes)
            throw new InvalidDataException("HAR file is over 64 MB.");

        using var document = JsonDocument.Parse(stream, new JsonDocumentOptions
        {
            AllowTrailingCommas = true,
            CommentHandling = JsonCommentHandling.Skip
        });

        if (!document.RootElement.TryGetProperty("log", out var log) || log.ValueKind != JsonValueKind.Object)
            throw new InvalidDataException("HAR is missing log.");
        if (!log.TryGetProperty("entries", out var entries) || entries.ValueKind != JsonValueKind.Array)
            throw new InvalidDataException("HAR is missing log.entries.");

        var pages = 0;
        if (log.TryGetProperty("pages", out var pageArray) && pageArray.ValueKind == JsonValueKind.Array)
            pages = pageArray.GetArrayLength();

        var warnings = new List<string>();
        if (!log.TryGetProperty("version", out var version) || version.GetString() != "1.2")
            warnings.Add("log.version is not 1.2.");

        return new LogReadResult(LogFormat.Har, entries.GetArrayLength(), pages, [], warnings);
    }
}
