using System.Text.Json;
using System.Text.Json.Nodes;
using Vestigium.Helpers;

namespace Vestigium.Helpers.Json;

internal static class JsonIO
{
    internal const int StreamBufferSize = 64 * 1024;

    internal static JsonNode Read(string path)
    {
        using var stream = new FileStream(
            path,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            StreamBufferSize,
            FileOptions.SequentialScan);
        return JsonHelper.Parse(stream);
    }

    internal static int Write(string path, JsonNode node, bool indented, bool atomic)
    {
        var parent = Path.GetDirectoryName(path);
        if (!string.IsNullOrWhiteSpace(parent))
            Directory.CreateDirectory(parent);

        if (!atomic)
            return WriteTo(path, node, indented, FileMode.Create);

        var temp = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            var bytes = WriteTo(temp, node, indented, FileMode.CreateNew);
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

    private static int WriteTo(string path, JsonNode node, bool indented, FileMode mode)
    {
        using var stream = new FileStream(
            path,
            mode,
            FileAccess.Write,
            FileShare.None,
            StreamBufferSize,
            FileOptions.SequentialScan);
        using (var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = indented }))
        {
            node.WriteTo(writer);
            writer.Flush();
        }

        stream.Flush(flushToDisk: true);
        return checked((int)stream.Length);
    }
}
