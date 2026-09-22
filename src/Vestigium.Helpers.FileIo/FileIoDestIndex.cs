using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Vestigium.Helpers.FileIo;

/// <summary>
/// Dest unique-content index. One jsonl per destination root under IndexRoot.
/// Audit Mode must not write. v1 keeps a dest file row even if the file is later deleted;
/// SkipDuplicate still requires a live dest file on the merge walk unless the digest is already loaded.
/// </summary>
internal static class FileIoDestIndex
{
    static readonly JsonSerializerOptions Json = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    internal sealed record Row(string Path, long Size, string Mtime, string Digest);

    public static void LoadInto(string indexFile, ConcurrentDictionary<string, string> destIndex)
    {
        if (!File.Exists(indexFile))
            return;
        foreach (var line in File.ReadLines(indexFile))
        {
            if (string.IsNullOrWhiteSpace(line))
                continue;
            Row? row;
            try
            {
                row = JsonSerializer.Deserialize<Row>(line, Json);
            }
            catch (JsonException)
            {
                continue;
            }
            if (row is null || string.IsNullOrWhiteSpace(row.Digest) || string.IsNullOrWhiteSpace(row.Path))
                continue;
            destIndex[row.Digest] = row.Path;
        }
    }

    public static void Append(string indexFile, Row row)
    {
        var dir = Path.GetDirectoryName(indexFile);
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);
        File.AppendAllText(indexFile, JsonSerializer.Serialize(row, Json) + Environment.NewLine);
    }
}
