using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Vestigium.Helpers;
using Vestigium.Logging;

namespace Vestigium.Helpers.Json;

/// <summary>
/// System.Text.Json helpers for payload documents. Phase 2: typed serialize, path parse, in-memory session.
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
            HelperLog.Information(app, VestigiumStatus.Pending, HelperLog.Subcategories.Session, $"Creating a blank JSON session={sessionId}");
            var stored = string.IsNullOrWhiteSpace(path) ? null : path.Trim();
            return new JsonSession(
                new JsonObject(),
                stored,
                JsonDocumentKind.Json,
                options ?? new JsonSessionOptions(),
                sessionId);
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
