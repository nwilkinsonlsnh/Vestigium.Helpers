using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Vestigium.Helpers;

namespace Vestigium.Helpers.Json;

internal static class JsonIO
{
    internal const int StreamBufferSize = 64 * 1024;

    internal static JsonDocumentKind KindFromPath(string? path)
        => !string.IsNullOrWhiteSpace(path) && path.EndsWith(".jsonl", StringComparison.OrdinalIgnoreCase)
            ? JsonDocumentKind.Jsonl
            : JsonDocumentKind.Json;

    internal static JsonNode Read(string path)
    {
        using var stream = OpenRead(path);
        return JsonHelper.Parse(stream);
    }

    internal static JsonArray ReadJsonl(string path)
    {
        using var stream = OpenRead(path);
        RejectBom(stream);
        using var reader = new StreamReader(stream, new UTF8Encoding(false), detectEncodingFromByteOrderMarks: false, StreamBufferSize);
        var records = new JsonArray();
        var index = 0;
        while (reader.ReadLine() is { } line)
        {
            if (string.IsNullOrWhiteSpace(line))
                continue;
            JsonNode? node;
            try
            {
                node = JsonNode.Parse(line, JsonCodec.NodeOptions, JsonCodec.DocumentOptions);
            }
            catch (JsonException)
            {
                HelperLog.Reject($"jsonl line is not RFC 8259 index={index}");
                throw;
            }

            records.Add(node);
            index++;
        }

        return records;
    }

    internal static int Write(string path, JsonNode node, bool indented, bool atomic)
        => WriteAtomic(path, atomic, dest => WriteJson(dest, node, indented));

    internal static int WriteJsonl(string path, JsonArray records, bool atomic)
        => WriteAtomic(path, atomic, dest => WriteJsonlTo(dest, records));

    internal static void RejectCollision(string path, JsonCollision collision, bool replaceInPlace)
    {
        if (replaceInPlace || collision == JsonCollision.Overwrite)
            return;
        if (!File.Exists(path))
            return;
        HelperLog.Reject($"dest exists path={path}");
        throw new IOException($"Destination already exists: {path}.");
    }

    internal static bool SamePath(string? left, string right)
    {
        if (string.IsNullOrWhiteSpace(left))
            return false;
        var a = Path.GetFullPath(left);
        var b = Path.GetFullPath(right);
        return OperatingSystem.IsWindows()
            ? string.Equals(a, b, StringComparison.OrdinalIgnoreCase)
            : string.Equals(a, b, StringComparison.Ordinal);
    }

    internal static string ResolveExportFile(string directory, string stem, JsonDocumentKind kind)
    {
        var ext = kind == JsonDocumentKind.Jsonl ? ".jsonl" : ".json";
        var file = stem.Trim();
        foreach (var ch in Path.GetInvalidFileNameChars())
            file = file.Replace(ch, '_');
        if (file is "." or ".." || string.IsNullOrWhiteSpace(file))
        {
            HelperLog.Reject("stem is not a file name");
            throw new ArgumentException("Export stem must be a file name.", nameof(stem));
        }

        if (!file.EndsWith(".json", StringComparison.OrdinalIgnoreCase)
            && !file.EndsWith(".jsonl", StringComparison.OrdinalIgnoreCase))
            file += ext;

        var dest = Path.GetFullPath(Path.Combine(directory, file));
        var root = Path.GetFullPath(directory);
        var prefix = root.EndsWith(Path.DirectorySeparatorChar) ? root : root + Path.DirectorySeparatorChar;
        if (!dest.StartsWith(prefix, StringComparison.Ordinal) && !string.Equals(dest, root, StringComparison.Ordinal))
        {
            HelperLog.Reject("export path escaped the export folder");
            throw new ArgumentException("Export stem must stay under the export folder.", nameof(stem));
        }

        return dest;
    }

    private static FileStream OpenRead(string path)
        => new(path, FileMode.Open, FileAccess.Read, FileShare.Read, StreamBufferSize, FileOptions.SequentialScan);

    private static int WriteAtomic(string path, bool atomic, Func<string, int> write)
    {
        var parent = Path.GetDirectoryName(path);
        if (!string.IsNullOrWhiteSpace(parent))
            Directory.CreateDirectory(parent);

        if (!atomic)
            return write(path);

        var temp = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            var bytes = write(temp);
            File.Move(temp, path, overwrite: true);
            return bytes;
        }
        catch
        {
            if (File.Exists(temp))
            {
                try { File.Delete(temp); }
                catch (IOException) { }
            }

            throw;
        }
    }

    private static int WriteJson(string path, JsonNode node, bool indented)
    {
        using var stream = OpenWrite(path);
        using (var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = indented }))
        {
            node.WriteTo(writer);
            writer.Flush();
        }

        stream.Flush(flushToDisk: true);
        return checked((int)stream.Length);
    }

    private static int WriteJsonlTo(string path, JsonArray records)
    {
        using var stream = OpenWrite(path);
        using (var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = false }))
        {
            foreach (var record in records)
            {
                if (record is null)
                    writer.WriteNullValue();
                else
                    record.WriteTo(writer);
                writer.Flush();
                stream.WriteByte((byte)'\n');
                writer.Reset();
            }
        }

        stream.Flush(flushToDisk: true);
        return checked((int)stream.Length);
    }

    private static FileStream OpenWrite(string path)
        => new(path, FileMode.Create, FileAccess.Write, FileShare.None, StreamBufferSize, FileOptions.SequentialScan);

    private static void RejectBom(Stream stream)
    {
        if (!stream.CanSeek)
            return;
        var mark = stream.Position;
        Span<byte> header = stackalloc byte[3];
        var read = stream.Read(header);
        stream.Position = mark;
        if (read >= 3 && header[0] == 0xEF && header[1] == 0xBB && header[2] == 0xBF)
        {
            HelperLog.Reject("json has a BOM");
            throw new JsonException("RFC 8259 JSON must be UTF-8 without a BOM.");
        }
    }
}
