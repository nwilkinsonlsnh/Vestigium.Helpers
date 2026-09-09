using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Vestigium.Helpers;
using Vestigium.Logging;

namespace Vestigium.Helpers.Json;

/// <summary>
/// System.Text.Json helpers for payload documents. Phase 5: sparse HelperLog, Probe %TEMP% only.
/// The class library never calls <see cref="VestigiumLogger.Initialize"/>.
/// </summary>
public static class JsonHelper
{
    public static string Identity => "Vestigium.Helpers.Json";

    public static string Probe()
    {
        var app = HelperLog.AppIds.Json;
        using var scope = HelperLog.Begin(app, HelperLog.Subcategories.Probe, "Probe");
        HelperLog.Information(app, VestigiumStatus.Pending, HelperLog.Subcategories.Probe, "Serializing a demo payload.");
        var json = ToJson(new { identity = Identity, probe = true });
        _ = Parse(json);
        HelperLog.Information(app, VestigiumStatus.Success, HelperLog.Subcategories.Probe, "JSON probe complete. Identity=" + Identity);
        HelperLog.Exit(app, HelperLog.Subcategories.Probe, "Probe", $"chars={json.Length}");
        return Identity;
    }

    public static string DefaultExportDirectory()
    {
        if (!string.IsNullOrWhiteSpace(JsonTestHooks.ExportRoot))
            return Path.GetFullPath(JsonTestHooks.ExportRoot);

        var desktop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
        if (string.IsNullOrWhiteSpace(desktop))
            desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
        if (string.IsNullOrWhiteSpace(desktop))
        {
            var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            desktop = Path.Combine(string.IsNullOrWhiteSpace(home) ? "." : home, "Desktop");
        }

        return Path.Combine(desktop, "Vestigium", "Exports", "Json");
    }

    public static string NewExportPath(string? stem = null, JsonDocumentKind kind = JsonDocumentKind.Json)
    {
        var stamp = DateTime.Now.ToString("yyyyMMdd-HHmmss");
        var name = string.IsNullOrWhiteSpace(stem)
            ? $"vestigium-Json-{stamp}"
            : stem.Trim();
        return JsonIO.ResolveExportFile(DefaultExportDirectory(), name, kind);
    }

    public static string ToJson<T>(T value, JsonWriteOptions? options = null)
    {
        var app = HelperLog.AppIds.Json;
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

    public static T FromJson<T>(string json, JsonReadOptions? options = null)
    {
        var app = HelperLog.AppIds.Json;
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

    public static JsonNode Parse(string json)
    {
        var app = HelperLog.AppIds.Json;
        using var scope = HelperLog.Begin(app, HelperLog.Subcategories.Document, "Parse");
        try
        {
            var text = RequireRfc8259Text(json);
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

    public static JsonNode Parse(Stream stream)
    {
        var app = HelperLog.AppIds.Json;
        using var scope = HelperLog.Begin(app, HelperLog.Subcategories.Document, "ParseStream");
        try
        {
            var input = HelperGuard.NotNull(stream, nameof(stream));
            HelperGuard.Require(input.CanRead, nameof(stream), "Stream must be readable.");
            RejectBom(input);
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

    public static JsonSession Create(string? path = null, JsonSessionOptions? options = null)
    {
        var app = HelperLog.AppIds.Json;
        var sessionId = HelperLog.NewId();
        using var scope = HelperLog.Begin(app, HelperLog.Subcategories.Session, "Create", $"session={sessionId}", sessionId);
        try
        {
            var stored = string.IsNullOrWhiteSpace(path) ? null : Path.GetFullPath(path.Trim());
            var kind = JsonIO.KindFromPath(stored);
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

    public static JsonSession Open(string path, JsonSessionOptions? options = null)
    {
        var app = HelperLog.AppIds.Json;
        var sessionId = HelperLog.NewId();
        var target = Path.GetFullPath(HelperGuard.FileExists(path, nameof(path)));
        using var scope = HelperLog.Begin(app, HelperLog.Subcategories.Session, "Open", $"path={target} session={sessionId}", sessionId);
        try
        {
            var node = JsonIO.Read(target);
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

    public static JsonSession OpenJsonl(string path, JsonSessionOptions? options = null)
    {
        var app = HelperLog.AppIds.Json;
        var sessionId = HelperLog.NewId();
        var target = Path.GetFullPath(HelperGuard.FileExists(path, nameof(path)));
        using var scope = HelperLog.Begin(app, HelperLog.Subcategories.Jsonl, "OpenJsonl", $"path={target} session={sessionId}", sessionId);
        try
        {
            var records = JsonIO.ReadJsonl(target);
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

    public static JsonSession OpenExport(string stem, JsonDocumentKind kind = JsonDocumentKind.Json, JsonSessionOptions? options = null)
    {
        var name = HelperGuard.NotBlank(stem, nameof(stem));
        var target = JsonIO.ResolveExportFile(DefaultExportDirectory(), name, kind);
        return kind == JsonDocumentKind.Jsonl ? OpenJsonl(target, options) : Open(target, options);
    }

    public static void WriteFile<T>(string path, T value, JsonWriteOptions? options = null)
    {
        var app = HelperLog.AppIds.Json;
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
            JsonIO.RejectCollision(target, opts.Collision, replaceInPlace: false);
            var node = JsonCodec.ToNode(value)
                ?? throw new JsonException("RFC 8259 JSON null is not a document root.");
            var bytes = JsonIO.Write(target, node, opts.WriteIndented, opts.AtomicWrite);
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

    private static string RequireRfc8259Text(string? json)
    {
        var text = HelperGuard.NotBlank(json, nameof(json));
        if (text[0] == '\uFEFF')
        {
            HelperLog.Reject("json has a BOM");
            throw new JsonException("RFC 8259 JSON must be UTF-8 without a BOM.");
        }

        return text;
    }

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
