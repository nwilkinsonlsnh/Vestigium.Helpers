using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Vestigium.Helpers.Json;
using Vestigium.Logging;

namespace Vestigium.Helpers.Tests;

[Collection("Logger")]
public sealed class JsonlTests : IDisposable
{
    private readonly string _root;

    public JsonlTests()
    {
        HelperLog.Shutdown();
        _root = Path.Combine(Path.GetTempPath(), "VestigiumJsonlTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_root);
        JsonTestHooks.ExportRoot = _root;
    }

    public void Dispose()
    {
        JsonTestHooks.ExportRoot = null;
        HelperLog.Shutdown();
        if (Directory.Exists(_root))
            Directory.Delete(_root, recursive: true);
    }

    [Fact]
    public void Three_object_lines_enumerate_append_and_save_rewrites_four()
    {
        var path = Path.Combine(_root, "payload.jsonl");
        File.WriteAllText(path, """{"code":"alpha","ms":12}""" + "\n" + """{"code":"beta","ms":18}""" + "\n" + """{"code":"gamma","ms":9}""" + "\n", new UTF8Encoding(false));
        using var doc = JsonHelper.OpenJsonl(path);
        Assert.Equal(JsonDocumentKind.Jsonl, doc.Kind);
        Assert.Equal(3, doc.RecordCount);
        Assert.Equal("beta", doc.Get<string>("[1].code"));
        Assert.Equal("beta", doc.Get<string>("/1/code"));
        Assert.Equal("alpha", doc.Record(0)!["code"]!.GetValue<string>());

        doc.AppendRecord(JsonNode.Parse("""{"code":"delta","ms":4}""")!);
        Assert.Equal(4, doc.RecordCount);
        Assert.True(doc.HasUncommittedWork);
        doc.Commit();
        doc.Save();

        var lines = File.ReadAllLines(path);
        Assert.Equal(4, lines.Length);
        Assert.Contains("\"code\":\"delta\"", lines[3]);
        Assert.DoesNotContain('\n', lines[0]);
        using var reopened = JsonHelper.OpenJsonl(path);
        Assert.Equal(4, reopened.RecordCount);
        Assert.Equal("delta", reopened.Get<string>("[3].code"));
    }

    [Fact]
    public void Primitive_line_is_legal_and_set_on_it_throws()
    {
        var path = Path.Combine(_root, "scalars.jsonl");
        File.WriteAllText(path, "42\ntrue\n\"ok\"\n");
        using var doc = JsonHelper.OpenJsonl(path);
        Assert.Equal(3, doc.RecordCount);
        Assert.Equal(42, doc.Record(0)!.GetValue<int>());
        Assert.True(doc.Record(1)!.GetValue<bool>());
        Assert.Equal("ok", doc.Record(2)!.GetValue<string>());
        Assert.Throws<InvalidOperationException>(() => doc.Set("[0].code", "nope"));
    }

    [Fact]
    public void Empty_lines_are_skipped_truncated_last_line_fails()
    {
        var skip = Path.Combine(_root, "skip.jsonl");
        File.WriteAllText(skip, "{\"code\":\"a\"}\n\n{\"code\":\"b\"}\r\n\r\n");
        using (var doc = JsonHelper.OpenJsonl(skip))
            Assert.Equal(2, doc.RecordCount);

        var bad = Path.Combine(_root, "truncated.jsonl");
        File.WriteAllText(bad, "{\"code\":\"ok\"}\n{\"code\":");
        Assert.ThrowsAny<JsonException>(() => JsonHelper.OpenJsonl(bad));
    }

    [Fact]
    public void Bom_jsonl_is_rejected()
    {
        var path = Path.Combine(_root, "bom.jsonl");
        File.WriteAllBytes(path, new byte[] { 0xEF, 0xBB, 0xBF }.Concat(Encoding.UTF8.GetBytes("""{"code":"ok"}""" + "\n")).ToArray());
        Assert.ThrowsAny<JsonException>(() => JsonHelper.OpenJsonl(path));
    }

    [Fact]
    public void Create_jsonl_is_an_empty_list_append_on_json_throws()
    {
        var path = Path.Combine(_root, "new.jsonl");
        using var jsonl = JsonHelper.Create(path);
        Assert.Equal(JsonDocumentKind.Jsonl, jsonl.Kind);
        Assert.Equal(0, jsonl.RecordCount);
        jsonl.AppendRecord(JsonNode.Parse("""{"code":"ok"}""")!);
        jsonl.Commit();
        jsonl.Save();
        Assert.Equal(new[] { """{"code":"ok"}""" }, File.ReadAllLines(path));

        using var json = JsonHelper.Create();
        Assert.Throws<InvalidOperationException>(() => json.AppendRecord(JsonNode.Parse("""{"a":1}""")!));
    }

    [Fact]
    public void OpenExport_jsonl_and_logs_counts_never_bodies()
    {
        var logDir = Path.Combine(_root, "logs");
        Directory.CreateDirectory(logDir);
        HelperLog.InitializeHost(HelperLog.AppIds.Json, cfg => cfg.LogDirectory = logDir);
        const string secret = "hunter2-jsonl-must-not-log";
        var path = JsonHelper.NewExportPath("payload-records", JsonDocumentKind.Jsonl);
        File.WriteAllText(path, """{"code":"keep"}""" + "\n");
        using var doc = JsonHelper.OpenExport("payload-records", JsonDocumentKind.Jsonl);
        doc.AppendRecord(JsonNode.Parse($$"""{"code":"{{secret}}"}""")!);
        doc.Commit();
        doc.Save();
        var lines = HelperLog.RecentJsonLines;
        Assert.DoesNotContain(lines, l => l.Contains(secret));
        Assert.Contains(lines, l => l.Contains("\"SUBCATEGORY\":\"Jsonl\"") && l.Contains("records="));
        Assert.Contains(lines, l => l.Contains("AppendRecord") && l.Contains("index=1"));
        Assert.All(lines, line => Assert.DoesNotContain("\"EXCEPTION\":\"", line.Replace("\"EXCEPTION\":null", "")));
        using var reopened = JsonHelper.OpenJsonl(path);
        Assert.Equal(2, reopened.RecordCount);
    }
}
