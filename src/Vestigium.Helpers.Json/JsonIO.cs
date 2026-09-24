using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Vestigium.Helpers;

namespace Vestigium.Helpers.Json;

internal static class JsonIo
{
    internal const int StreamBufferSize = 64 * 1024;
    internal const long DefaultMaxDocumentBytes = 32L * 1024 * 1024;
    internal const int DefaultMaxJsonlLineBytes = 1024 * 1024;

    internal static long MaxDocumentBytes => JsonTestHooks.MaxDocumentBytes ?? DefaultMaxDocumentBytes;

    internal static int MaxJsonlLineBytes => JsonTestHooks.MaxJsonlLineBytes ?? DefaultMaxJsonlLineBytes;

    private static StringComparison PathCompare => OperatingSystem.IsWindows()
        ? StringComparison.OrdinalIgnoreCase
        : StringComparison.Ordinal;

    internal static JsonDocumentKind KindFromPath(string? path)
        => !string.IsNullOrWhiteSpace(path) && path.EndsWith(".jsonl", StringComparison.OrdinalIgnoreCase)
            ? JsonDocumentKind.Jsonl
            : JsonDocumentKind.Json;

    internal static JsonNode Read(string path)
    {
        RejectFileTooLarge(path);
        using var stream = OpenRead(path);
        RejectBom(stream);
        JsonNode? node;
        try
        {
            node = JsonNode.Parse(stream, JsonCodec.NodeOptions, JsonCodec.DocumentOptions);
        }
        catch (JsonException)
        {
            HelperLog.Reject("json is not RFC 8259");
            throw;
        }

        if (node is not null) return node;
        HelperLog.Reject("json is JSON null");
        throw new JsonException("RFC 8259 JSON null is not a document root for Parse.");

    }

    internal static JsonArray ReadJsonl(string path)
    {
        RejectFileTooLarge(path);
        using var stream = OpenRead(path);
        RejectBom(stream);
        var terminated = EndsWithNewline(stream);
        var records = new JsonArray();
        var index = 0;
        while (ReadJsonlLine(stream, index) is { } line)
        {
            if (string.IsNullOrWhiteSpace(line))
                continue;

            JsonNode? node;
            try
            {
                node = JsonNode.Parse(line, JsonCodec.NodeOptions, JsonCodec.DocumentOptions);
            }
            catch (JsonException ex)
            {
                var truncated = !terminated && stream.Position >= stream.Length;
                HelperLog.Reject(truncated
                    ? $"jsonl line is truncated index={index}"
                    : $"jsonl line is not RFC 8259 index={index}");
                if (truncated)
                    throw new JsonException($"JSONL last line is truncated at index {index}.", ex);
                throw;
            }

            records.Add(node);
            index++;
        }

        return records;
    }

    internal static void EnsureStreamWithinCap(Stream stream)
    {
        if (!stream.CanSeek)
            return;

        RejectDocumentTooLarge(stream.Length - stream.Position);
    }

    internal static void RejectDocumentTooLarge(long bytes)
    {
        if (bytes <= MaxDocumentBytes)
            return;

        HelperLog.Reject($"document exceeds cap bytes={bytes} cap={MaxDocumentBytes}");
        throw new JsonException($"JSON document exceeds the {MaxDocumentBytes} byte cap.");
    }

    internal static long Write(string path, JsonNode node, bool indented, bool atomic)
        => WriteStaged(path, dest => WriteJson(dest, node, indented));

    internal static long WriteJsonl(string path, JsonArray records, bool atomic)
        => WriteStaged(path, dest => WriteJsonlTo(dest, records));

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
        return string.Equals(a, b, PathCompare);
    }

    internal static string ResolveExportFile(string directory, string stem, JsonDocumentKind kind)
    {
        var ext = kind == JsonDocumentKind.Jsonl ? ".jsonl" : ".json";
        var file = stem.Trim();
        file = Path.GetInvalidFileNameChars().Aggregate(file, (current, ch) => current.Replace(ch, '_'));
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
        if (dest.StartsWith(prefix, PathCompare) || string.Equals(dest, root, PathCompare))
            return dest;

        HelperLog.Reject("export path escaped the export folder");
        throw new ArgumentException("Export stem must stay under the export folder.", nameof(stem));
    }

    private static bool EndsWithNewline(Stream stream)
    {
        if (!stream.CanSeek || stream.Length == 0)
            return true;

        var mark = stream.Position;
        stream.Seek(-1, SeekOrigin.End);
        var last = stream.ReadByte();
        stream.Position = mark;
        return last == '\n';
    }

    private static FileStream OpenRead(string path)
        => new(path, FileMode.Open, FileAccess.Read, FileShare.Read, StreamBufferSize, FileOptions.SequentialScan);

    private static long WriteStaged(string path, Func<string, long> write)
    {
        var parent = Path.GetDirectoryName(path);
        if (!string.IsNullOrWhiteSpace(parent))
            Directory.CreateDirectory(parent);

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

    private static long WriteJson(string path, JsonNode node, bool indented)
    {
        using var stream = OpenWrite(path);
        using (var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = indented }))
        {
            node.WriteTo(writer);
            writer.Flush();
        }

        stream.Flush(flushToDisk: true);
        RejectWrittenTooLarge(path, stream.Length);
        return stream.Length;
    }

    private static long WriteJsonlTo(string path, JsonArray records)
    {
        using var stream = OpenWrite(path);
        using (var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = false }))
        {
            foreach (var record in records)
            {
                var start = stream.Position;
                if (record is null)
                    writer.WriteNullValue();
                else
                    record.WriteTo(writer);
                writer.Flush();
                stream.WriteByte((byte)'\n');
                writer.Reset();
                var lineBytes = stream.Position - start;
                if (lineBytes > MaxJsonlLineBytes)
                {
                    HelperLog.Reject($"jsonl line exceeds cap path={path} bytes={lineBytes} cap={MaxJsonlLineBytes}");
                    throw new JsonException($"JSONL line exceeds the {MaxJsonlLineBytes} byte cap.");
                }
            }
        }

        stream.Flush(flushToDisk: true);
        RejectWrittenTooLarge(path, stream.Length);
        return stream.Length;
    }

    private static FileStream OpenWrite(string path)
        => new(path, FileMode.Create, FileAccess.Write, FileShare.None, StreamBufferSize, FileOptions.SequentialScan);


    private static void RejectFileTooLarge(string path)
    {
        var bytes = new FileInfo(path).Length;
        if (bytes <= MaxDocumentBytes)
            return;

        HelperLog.Reject($"document exceeds cap path={path} bytes={bytes} cap={MaxDocumentBytes}");
        throw new JsonException($"JSON document exceeds the {MaxDocumentBytes} byte cap.");
    }

    private static void RejectWrittenTooLarge(string path, long bytes)
    {
        if (bytes <= MaxDocumentBytes)
            return;

        HelperLog.Reject($"document exceeds cap path={path} bytes={bytes} cap={MaxDocumentBytes}");
        throw new JsonException($"JSON document exceeds the {MaxDocumentBytes} byte cap.");
    }

    private static string? ReadJsonlLine(Stream stream, int index)
    {
        var buffer = new MemoryStream();
        while (true)
        {
            var next = stream.ReadByte();
            if (next < 0)
                return buffer.Length == 0 ? null : Encoding.UTF8.GetString(buffer.ToArray());

            if (next == '\n')
            {
                var raw = buffer.ToArray();
                var length = raw.Length;
                if (length > 0 && raw[length - 1] == (byte)'\r')
                    length--;
                return Encoding.UTF8.GetString(raw, 0, length);
            }

            buffer.WriteByte((byte)next);
            if (buffer.Length <= MaxJsonlLineBytes)
                continue;

            HelperLog.Reject($"jsonl line exceeds cap index={index} bytes={buffer.Length} cap={MaxJsonlLineBytes}");
            throw new JsonException($"JSONL line exceeds the {MaxJsonlLineBytes} byte cap at index {index}.");
        }
    }

    private static void RejectBom(Stream stream)
    {
        if (!stream.CanSeek)
            return;
        var mark = stream.Position;
        Span<byte> header = stackalloc byte[3];
        var read = stream.Read(header);
        stream.Position = mark;
        if (read < 3 || header[0] != 0xEF || header[1] != 0xBB || header[2] != 0xBF) return;
        HelperLog.Reject("json has a BOM");
        throw new JsonException("RFC 8259 JSON must be UTF-8 without a BOM.");
    }
}
