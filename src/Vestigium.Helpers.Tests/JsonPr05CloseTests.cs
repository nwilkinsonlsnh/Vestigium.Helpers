using System.Reflection;
using System.Text;
using Vestigium.Helpers.Json;
using Vestigium.Logging;

namespace Vestigium.Helpers.Tests;

[Collection("Logger")]
public sealed class JsonPr05CloseTests : IDisposable
{
    private readonly string _root;

    public JsonPr05CloseTests()
    {
        VestigiumLogger.Shutdown();
        _root = Path.Combine(Path.GetTempPath(), "VestigiumJsonPr05Close", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_root);
        JsonTestHooks.ExportRoot = _root;
    }

    public void Dispose()
    {
        JsonTestHooks.ExportRoot = null;
        VestigiumLogger.Shutdown();
        if (Directory.Exists(_root))
            Directory.Delete(_root, recursive: true);
    }

    [Fact]
    public void Public_surface_has_span_parse_and_compare_not_apply()
    {
        var helper = typeof(JsonHelper);
        Assert.NotNull(helper.GetMethod("Parse", [typeof(ReadOnlySpan<byte>)]));
        Assert.NotNull(helper.GetMethod("Compare", [typeof(string), typeof(string)]));
        Assert.Null(helper.GetMethod("Apply", BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance));

        var patch = typeof(JsonPatch);
        Assert.True(patch.GetMethod("Compare", BindingFlags.Public | BindingFlags.Static)!.IsPublic);
        Assert.Null(patch.GetMethod("Apply", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance));
        Assert.DoesNotContain(
            typeof(JsonPatchOperation).GetProperties(),
            p => p.Name is "From" or "from");
    }

    [Fact]
    public void Event_ids_stay_in_block_and_count_by_five()
    {
        var ids = typeof(JsonEvents)
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(f => f.FieldType == typeof(int) && f.Name is not ("BlockStart" or "BlockEnd"))
            .Select(f => (int)f.GetValue(null)!)
            .ToArray();
        Assert.NotEmpty(ids);
        Assert.All(ids, id =>
        {
            Assert.InRange(id, JsonEvents.BlockStart, JsonEvents.BlockEnd);
            Assert.Equal(0, id % 5);
        });
        Assert.Equal(ids.Length, ids.Distinct().Count());
        Assert.Contains(JsonEvents.ProbeEnter, ids);
        Assert.Contains(JsonEvents.ProbeComplete, ids);
        Assert.Equal(13500, JsonEvents.ProbeEnter);
        Assert.Equal(13505, JsonEvents.ProbeComplete);
        Assert.Equal(13625, JsonEvents.OperationWarning);
    }

    [Fact]
    public void Compare_logs_ops_and_paths_never_bodies()
    {
        var logDir = Path.Combine(_root, "logs");
        Directory.CreateDirectory(logDir);
        VestigiumLogger.Initialize(cfg =>
        {
            cfg.AppId = JsonCatalog.AppId;
            cfg.LogDirectory = logDir;
            cfg.MinimumDiskLevel = VestigiumLogLevel.Debug;
            cfg.OperationsLogEnabled = false;
            JsonCatalog.Register(cfg);
        });

        const string secret = "hunter2-compare-must-not-log";
        var left = Path.Combine(_root, "left.json");
        var right = Path.Combine(_root, "right.json");
        JsonHelper.WriteFile(left, new { Level = "Information" });
        JsonHelper.WriteFile(right, new { Level = secret });
        var patch = JsonHelper.Compare(left, right);
        Assert.Equal(1, patch.Count);

        VestigiumLogger.Flush();
        var lines = VestigiumLogger.RecentJsonLines;
        Assert.DoesNotContain(lines, l => l.Contains(secret));
        Assert.Contains(lines, l =>
            l.Contains("\"SUBCATEGORY\":\"Diff\"") && l.Contains("ops=1"));
        Assert.Contains(lines, l => l.Contains("\"EVENTID\":13565") || l.Contains("\"EVENTID\":13570"));
        Assert.All(lines, line => Assert.DoesNotContain("\"EXCEPTION\":\"", line.Replace("\"EXCEPTION\":null", "")));
    }

    [Fact]
    public void Span_parse_and_truncated_jsonl_still_hold()
    {
        var node = JsonHelper.Parse("{\"ok\":true}"u8);
        Assert.True(node["ok"]!.GetValue<bool>());

        var chopped = Path.Combine(_root, "chopped.jsonl");
        File.WriteAllText(chopped, "{\"n\":1}\n{", new UTF8Encoding(false));
        var ex = Assert.ThrowsAny<JsonException>(() => JsonHelper.OpenJsonl(chopped));
        Assert.Contains("truncated", ex.Message, StringComparison.OrdinalIgnoreCase);
    }
}
