using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Vestigium.Helpers;
using Vestigium.Logging;

namespace Vestigium.Helpers.Json;

/// <summary>
/// RFC 8259 JSON and JSONL helpers. The class library never calls
/// <see cref="VestigiumLogger.Initialize"/>.
/// </summary>
public static class JsonHelper
{
    /// <summary>Assembly identity returned by <see cref="Probe"/>.</summary>
    public static string Identity => "Vestigium.Helpers.Json";

    /// <summary>
    /// Serializes and parses a tiny in-memory payload. Does not write the export folder.
    /// </summary>
    /// <returns><see cref="Identity"/>.</returns>
    public static string Probe()
    {
        const string app = HelperLog.AppIds.Json;
        using var scope = HelperLog.Begin(app, HelperLog.Subcategories.Probe, "Probe");
        HelperLog.Information(app, VestigiumStatus.Pending, HelperLog.Subcategories.Probe, "Serializing a demo payload.");
        var json = ToJson(new { identity = Identity, probe = true });
        _ = Parse(json);
        HelperLog.Information(app, VestigiumStatus.Success, HelperLog.Subcategories.Probe, "JSON probe complete. Identity=" + Identity);
        HelperLog.Exit(app, HelperLog.Subcategories.Probe, "Probe", $"chars={json.Length}");
        return Identity;
    }

    /// <summary>
    /// Export folder: <c>%DESKTOP%\Vestigium\Exports\Json</c>, or the test hook when set.
    /// </summary>
    public static string DefaultExportDirectory()
    {
        if (!string.IsNullOrWhiteSpace(JsonTestHooks.ExportRoot))
            return Path.GetFullPath(JsonTestHooks.ExportRoot);

        var desktop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
        if (string.IsNullOrWhiteSpace(desktop))
            desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
        if (!string.IsNullOrWhiteSpace(desktop)) return Path.Combine(desktop, "Vestigium", "Exports", "Json");
        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        desktop = Path.Combine(string.IsNullOrWhiteSpace(home) ? "." : home, "Desktop");

        return Path.Combine(desktop, "Vestigium", "Exports", "Json");
    }

    /// <summary>
    /// Builds a path under <see cref="DefaultExportDirectory"/> without creating the file.
    /// </summary>
    public static string NewExportPath(string? stem = null, JsonDocumentKind kind = JsonDocumentKind.Json)
    {
        var stamp = DateTime.Now.ToString("yyyyMMdd-HHmmss");
        var name = string.IsNullOrWhiteSpace(stem)
            ? $"vestigium-Json-{stamp}"
            : stem.Trim();
        return JsonIo.ResolveExportFile(DefaultExportDirectory(), name, kind);
    }

    /// <summary>Serialize <paramref name="value"/> to an RFC 8259 string. Null value is rejected.</summary>
    public static string ToJson<T>(T value, JsonWriteOptions? options = null)
    {
        const string app = HelperLog.AppIds.Json;
        using var scope = HelperLog.Begin(app, HelperLog.Subcategories.Document, "ToJson", "type=" + typeof(T).Name);
        try
        {
            if (value is null)
            {
                HelperLog.Reject("value is null");
                throw new ArgumentNullException(nameof(value));
            }

            var indented = options?.WriteIndented ?? true;
            var json = JsonSerializer.Serialize(value, JsonCodec.Write(indented));
            var bytes = Encoding.UTF8.GetByteCount(json);
            HelperLog.Information(
                app,
                VestigiumStatus.Success,
                HelperLog.Subcategories.Document,
                $"ToJson type={typeof(T).Name} chars={json.Length} bytes={bytes} indented={indented}");
            return json;
        }
        catch (ArgumentNullException)
        {
            throw;
        }
        catch (JsonException)
        {
            HelperLog.Reject("json serialize failed type=" + typeof(T).Name);
            throw;
        }
        catch (Exception ex)
        {
            HelperLog.Trap(ex);
            throw;
        }
    }

    /// <summary>Deserialize RFC 8259 text. Rejects blank text and a leading BOM.</summary>
    public static T FromJson<T>(string json, JsonReadOptions? options = null)
    {
        const string app = HelperLog.AppIds.Json;
        using var scope = HelperLog.Begin(app, HelperLog.Subcategories.Document, "FromJson", "type=" + typeof(T).Name);
        try
        {
            var text = RequireRfc8259Text(json);
            T? value;
            try
            {
                value = JsonSerializer.Deserialize<T>(text, JsonCodec.Read(options));
            }
            catch (JsonException)
            {
                HelperLog.Reject("json is not RFC 8259");
                throw;
            }

            HelperLog.Information(
                app,
                VestigiumStatus.Success,
                HelperLog.Subcategories.Document,
                $"FromJson type={typeof(T).Name} chars={text.Length}");
            return value!;
        }
        catch (ArgumentException)
        {
            throw;
        }
        catch (JsonException)
        {
            throw;
        }
        catch (Exception ex)
        {
            HelperLog.Trap(ex);
            throw;
        }
    }

    /// <summary>Parse RFC 8259 text to a <see cref="JsonNode"/>. JSON null is not a document root. Honors the document cap.</summary>
    public static JsonNode Parse(string json)
    {
        const string app = HelperLog.AppIds.Json;
        using var scope = HelperLog.Begin(app, HelperLog.Subcategories.Document, "Parse");
        try
        {
            var text = RequireRfc8259Text(json);
            JsonIo.RejectDocumentTooLarge(Encoding.UTF8.GetByteCount(text));
            JsonNode? node;
            try
            {
                node = JsonNode.Parse(text, JsonCodec.NodeOptions, JsonCodec.DocumentOptions);
            }
            catch (JsonException)
            {
                HelperLog.Reject("json is not RFC 8259");
                throw;
            }

            if (node is null)
            {
                HelperLog.Reject("json is JSON null");
                throw new JsonException("RFC 8259 JSON null is not a document root for Parse.");
            }

            var kind = node is JsonObject ? "object" : node is JsonArray ? "array" : "value";
            HelperLog.Information(
                app,
                VestigiumStatus.Success,
                HelperLog.Subcategories.Document,
                $"Parse kind={kind} chars={text.Length}");
            return node;
        }
        catch (ArgumentException)
        {
            throw;
        }
        catch (JsonException)
        {
            throw;
        }
        catch (Exception ex)
        {
            HelperLog.Trap(ex);
            throw;
        }
    }

    /// <summary>
    /// Parse a readable UTF-8 stream. Rejects a BOM on a seekable stream and payloads over the document cap.
    /// </summary>
    public static JsonNode Parse(Stream stream)
    {
        const string app = HelperLog.AppIds.Json;
        using var scope = HelperLog.Begin(app, HelperLog.Subcategories.Document, "ParseStream");
        try
        {
            var input = HelperGuard.NotNull(stream, nameof(stream));
            HelperGuard.Require(input.CanRead, nameof(stream), "Stream must be readable.");
            RejectBom(input);
            JsonIo.EnsureStreamWithinCap(input);
            JsonNode? node;
            try
            {
                node = JsonNode.Parse(input, JsonCodec.NodeOptions, JsonCodec.DocumentOptions);
            }
            catch (JsonException)
            {
                HelperLog.Reject("json is not RFC 8259");
                throw;
            }

            if (node is null)
            {
                HelperLog.Reject("json is JSON null");
                throw new JsonException("RFC 8259 JSON null is not a document root for Parse.");
            }

            var kind = node is JsonObject ? "object" : node is JsonArray ? "array" : "value";
            HelperLog.Information(
                app,
                VestigiumStatus.Success,
                HelperLog.Subcategories.Document,
                $"ParseStream kind={kind}");
            return node;
        }
        catch (ArgumentException)
        {
            throw;
        }
        catch (JsonException)
        {
            throw;
        }
        catch (Exception ex)
        {
            HelperLog.Trap(ex);
            throw;
        }
    }

    /// <summary>Parse UTF-8 bytes. Rejects empty input, a leading BOM, and payloads over the document cap.</summary>
    public static JsonNode Parse(ReadOnlySpan<byte> utf8Json)
    {
        const string app = HelperLog.AppIds.Json;
        using var scope = HelperLog.Begin(app, HelperLog.Subcategories.Document, "ParseSpan");
        try
        {
            if (utf8Json.IsEmpty)
            {
                HelperLog.Reject("json is blank");
                throw new ArgumentException("Value is required.", nameof(utf8Json));
            }

            if (utf8Json.Length >= 3 && utf8Json[0] == 0xEF && utf8Json[1] == 0xBB && utf8Json[2] == 0xBF)
            {
                HelperLog.Reject("json has a BOM");
                throw new JsonException("RFC 8259 JSON must be UTF-8 without a BOM.");
            }

            JsonIo.RejectDocumentTooLarge(utf8Json.Length);
            JsonNode? node;
            try
            {
                node = JsonNode.Parse(utf8Json, JsonCodec.NodeOptions, JsonCodec.DocumentOptions);
            }
            catch (JsonException)
            {
                HelperLog.Reject("json is not RFC 8259");
                throw;
            }

            if (node is null)
            {
                HelperLog.Reject("json is JSON null");
                throw new JsonException("RFC 8259 JSON null is not a document root for Parse.");
            }

            var kind = node is JsonObject ? "object" : node is JsonArray ? "array" : "value";
            HelperLog.Information(
                app,
                VestigiumStatus.Success,
                HelperLog.Subcategories.Document,
                $"ParseSpan kind={kind} bytes={utf8Json.Length}");
            return node;
        }
        catch (ArgumentException)
        {
            throw;
        }
        catch (JsonException)
        {
            throw;
        }
        catch (Exception ex)
        {
            HelperLog.Trap(ex);
            throw;
        }
    }

    /// <summary>
    /// New empty session. A <c>.jsonl</c> path starts an empty list; otherwise an empty object.
    /// Does not create the file until Save.
    /// </summary>
    public static JsonSession Create(string? path = null, JsonSessionOptions? options = null)
    {
        const string app = HelperLog.AppIds.Json;
        var sessionId = HelperLog.NewId();
        using var scope = HelperLog.Begin(app, HelperLog.Subcategories.Session, "Create", $"session={sessionId}", sessionId);
        try
        {
            var stored = string.IsNullOrWhiteSpace(path) ? null : Path.GetFullPath(path.Trim());
            var kind = JsonIo.KindFromPath(stored);
            JsonNode root = kind == JsonDocumentKind.Jsonl ? new JsonArray() : new JsonObject();
            return new JsonSession(
                root,
                stored,
                kind,
                options ?? new JsonSessionOptions(),
                sessionId);
        }
        catch (Exception ex)
        {
            HelperLog.Trap(ex);
            throw;
        }
    }

    /// <summary>
    /// Open one RFC 8259 document. A <c>.jsonl</c> path is rejected; use <see cref="OpenJsonl"/>.
    /// </summary>
    public static JsonSession Open(string path, JsonSessionOptions? options = null)
    {
        const string app = HelperLog.AppIds.Json;
        var sessionId = HelperLog.NewId();
        var target = Path.GetFullPath(HelperGuard.FileExists(path, nameof(path)));
        using var scope = HelperLog.Begin(app, HelperLog.Subcategories.Session, "Open", $"path={target} session={sessionId}", sessionId);
        try
        {
            if (JsonIo.KindFromPath(target) == JsonDocumentKind.Jsonl)
            {
                HelperLog.Reject("Open is a single RFC 8259 document. Use OpenJsonl.");
                throw new ArgumentException("Open is a single RFC 8259 document. Use OpenJsonl.", nameof(path));
            }

            var node = JsonIo.Read(target);
            var bytes = new FileInfo(target).Length;
            HelperLog.Information(
                app,
                VestigiumStatus.Success,
                HelperLog.Subcategories.Document,
                $"Open kind={(node is JsonObject ? "object" : node is JsonArray ? "array" : "value")} path={target} bytes={bytes} session={sessionId}");
            return new JsonSession(node, target, JsonDocumentKind.Json, options ?? new JsonSessionOptions(), sessionId);
        }
        catch (ArgumentException)
        {
            throw;
        }
        catch (JsonException)
        {
            throw;
        }
        catch (Exception ex)
        {
            HelperLog.Trap(ex);
            throw;
        }
    }

    /// <summary>Open newline-delimited JSON. Empty lines are skipped. A truncated last line fails closed.</summary>
    public static JsonSession OpenJsonl(string path, JsonSessionOptions? options = null)
    {
        const string app = HelperLog.AppIds.Json;
        var sessionId = HelperLog.NewId();
        var target = Path.GetFullPath(HelperGuard.FileExists(path, nameof(path)));
        using var scope = HelperLog.Begin(app, HelperLog.Subcategories.Jsonl, "OpenJsonl", $"path={target} session={sessionId}", sessionId);
        try
        {
            var records = JsonIo.ReadJsonl(target);
            var bytes = new FileInfo(target).Length;
            HelperLog.Information(
                app,
                VestigiumStatus.Success,
                HelperLog.Subcategories.Jsonl,
                $"OpenJsonl path={target} records={records.Count} bytes={bytes} session={sessionId}");
            return new JsonSession(records, target, JsonDocumentKind.Jsonl, options ?? new JsonSessionOptions(), sessionId);
        }
        catch (ArgumentException)
        {
            throw;
        }
        catch (JsonException)
        {
            throw;
        }
        catch (Exception ex)
        {
            HelperLog.Trap(ex);
            throw;
        }
    }

    /// <summary>
    /// Open a file under <see cref="DefaultExportDirectory"/>. JSONL stems go through <see cref="OpenJsonl"/>.
    /// </summary>
    public static JsonSession OpenExport(string stem, JsonDocumentKind kind = JsonDocumentKind.Json, JsonSessionOptions? options = null)
    {
        var name = HelperGuard.NotBlank(stem, nameof(stem));
        var target = JsonIo.ResolveExportFile(DefaultExportDirectory(), name, kind);
        return kind == JsonDocumentKind.Jsonl ? OpenJsonl(target, options) : Open(target, options);
    }

    /// <summary>Serialize <paramref name="value"/> to <paramref name="path"/>. Null value is rejected.</summary>
    public static void WriteFile<T>(string path, T value, JsonWriteOptions? options = null)
    {
        const string app = HelperLog.AppIds.Json;
        var target = Path.GetFullPath(HelperGuard.NotBlank(path, nameof(path)));
        using var scope = HelperLog.Begin(app, HelperLog.Subcategories.Save, "WriteFile", "path=" + target);
        try
        {
            if (value is null)
            {
                HelperLog.Reject("value is null");
                throw new ArgumentNullException(nameof(value));
            }

            var opts = options ?? new JsonWriteOptions();
            JsonIo.RejectCollision(target, opts.Collision, replaceInPlace: false);
            var node = JsonCodec.ToNode(value)
                ?? throw new JsonException("RFC 8259 JSON null is not a document root.");
            var bytes = JsonIo.Write(target, node, opts.WriteIndented, opts.AtomicWrite);
            HelperLog.Information(
                app,
                VestigiumStatus.Success,
                HelperLog.Subcategories.Save,
                $"WriteFile path={target} bytes={bytes} collision={opts.Collision} atomic={opts.AtomicWrite}");
        }
        catch (ArgumentException)
        {
            throw;
        }
        catch (IOException)
        {
            throw;
        }
        catch (JsonException)
        {
            throw;
        }
        catch (Exception ex)
        {
            HelperLog.Trap(ex);
            throw;
        }
    }

    /// <summary>
    /// Diff two files of the same kind. Mixed JSON and JSONL is rejected.
    /// </summary>
    public static JsonPatch Compare(string leftPath, string rightPath)
    {
        const string app = HelperLog.AppIds.Json;
        var left = Path.GetFullPath(HelperGuard.FileExists(leftPath, nameof(leftPath)));
        var right = Path.GetFullPath(HelperGuard.FileExists(rightPath, nameof(rightPath)));
        using var scope = HelperLog.Begin(app, HelperLog.Subcategories.Diff, "Compare", $"left={left} right={right}");
        try
        {
            var leftKind = JsonIo.KindFromPath(left);
            var rightKind = JsonIo.KindFromPath(right);
            if (leftKind != rightKind)
            {
                HelperLog.Reject($"compare kind mismatch left={leftKind} right={rightKind}");
                throw new ArgumentException("Compare requires both paths to be the same document kind.");
            }

            JsonNode leftNode = leftKind == JsonDocumentKind.Jsonl
                ? JsonIo.ReadJsonl(left)
                : JsonIo.Read(left);
            JsonNode rightNode = rightKind == JsonDocumentKind.Jsonl
                ? JsonIo.ReadJsonl(right)
                : JsonIo.Read(right);
            var patch = JsonPatch.Compare(leftNode, rightNode);
            HelperLog.Information(
                app,
                VestigiumStatus.Success,
                HelperLog.Subcategories.Diff,
                $"Compare left={left} right={right} ops={patch.Count}");
            return patch;
        }
        catch (ArgumentException)
        {
            throw;
        }
        catch (JsonException)
        {
            throw;
        }
        catch (Exception ex)
        {
            HelperLog.Trap(ex);
            throw;
        }
    }

    private static string RequireRfc8259Text(string? json)
    {
        var text = HelperGuard.NotBlank(json, nameof(json));
        if (text[0] != '\uFEFF') return text;
        HelperLog.Reject("json has a BOM");
        throw new JsonException("RFC 8259 JSON must be UTF-8 without a BOM.");

    }

    private static void RejectBom(Stream stream)
    {
        if (!stream.CanSeek)
            return;
        var mark = stream.Position;
        Span<byte> header = stackalloc byte[3];
        var read = stream.Read(header);
        stream.Position = mark;
        switch (read)
        {
            case >= 3 when header[0] == 0xEF && header[1] == 0xBB && header[2] == 0xBF:
                HelperLog.Reject("json has a BOM");
                throw new JsonException("RFC 8259 JSON must be UTF-8 without a BOM.");
        }
    }
}
