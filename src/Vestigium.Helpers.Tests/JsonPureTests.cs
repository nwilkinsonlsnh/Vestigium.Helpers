using System.Text;
using System.Text.Json;
using Vestigium.Helpers.Json;
using Vestigium.Logging;

namespace Vestigium.Helpers.Tests;

[Collection("Logger")]
public sealed class JsonPureTests
{
    public JsonPureTests()
    {
        HelperLog.Shutdown();
    }

    [Fact]
    public void Identity_is_stable()
        => Assert.Equal("Vestigium.Helpers.Json", JsonHelper.Identity);

    [Fact]
    public void Probe_is_temp_only_and_logs_pending_then_success()
    {
        var dir = Path.Combine(Path.GetTempPath(), "VestigiumJsonTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        HelperLog.InitializeHost(HelperLog.AppIds.Json, cfg => cfg.LogDirectory = dir);
        try
        {
            Assert.Equal("Vestigium.Helpers.Json", JsonHelper.Probe());
            var lines = HelperLog.RecentJsonLines;
            Assert.Contains(lines, l => l.Contains("\"STATUS\":\"Pending\""));
            Assert.Contains(lines, l => l.Contains("\"STATUS\":\"Success\""));
            Assert.Contains(lines, l => l.Contains("\"APPID\":\"Json\""));
            Assert.StartsWith(Path.GetTempPath().TrimEnd(Path.DirectorySeparatorChar), dir);
        }
        finally
        {
            HelperLog.Shutdown();
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public void ToJson_is_pretty_camelCase()
    {
        var json = JsonHelper.ToJson(new ProbeDto { TimeoutSeconds = 15, Level = "Information" });
        Assert.Contains("\"timeoutSeconds\": 15", json);
        Assert.Contains("\"level\": \"Information\"", json);
        Assert.Contains('\n', json);
        Assert.DoesNotContain("TimeoutSeconds", json);
    }

    [Fact]
    public void FromJson_round_trips_typed_dto()
    {
        var json = JsonHelper.ToJson(new ProbeDto { TimeoutSeconds = 15, Level = "Information" });
        var back = JsonHelper.FromJson<ProbeDto>(json);
        Assert.Equal(15, back.TimeoutSeconds);
        Assert.Equal("Information", back.Level);
    }

    [Fact]
    public void Compact_write_has_no_newlines()
    {
        var json = JsonHelper.ToJson(new ProbeDto { TimeoutSeconds = 1, Level = "x" }, new JsonWriteOptions { WriteIndented = false });
        Assert.DoesNotContain('\n', json);
        Assert.Contains("\"timeoutSeconds\":1", json);
    }

    [Fact]
    public void Comments_are_rejected()
    {
        Assert.ThrowsAny<JsonException>(() => JsonHelper.Parse("""{ "a": 1 /* nope */ }"""));
        Assert.ThrowsAny<JsonException>(() => JsonHelper.FromJson<ProbeDto>("{ \"timeoutSeconds\": 1, // x\n\"level\": \"y\" }"));
    }

    [Fact]
    public void Trailing_commas_are_rejected()
        => Assert.ThrowsAny<JsonException>(() => JsonHelper.Parse("""{ "a": 1, }"""));

    [Fact]
    public void Bom_is_rejected()
    {
        Assert.Throws<JsonException>(() => JsonHelper.Parse("\uFEFF{\"a\":1}"));
        using var stream = new MemoryStream(new byte[] { 0xEF, 0xBB, 0xBF }.Concat(Encoding.UTF8.GetBytes("""{"a":1}""")).ToArray());
        Assert.Throws<JsonException>(() => JsonHelper.Parse(stream));
    }

    [Fact]
    public void Pointer_and_dotted_resolve_the_same_member()
    {
        var node = JsonHelper.Parse("""{"network":{"timeoutSeconds":15},"records":[{"code":"ok"}]}""");
        var pointer = JsonPath.Parse("/network/timeoutSeconds");
        var dotted = JsonPath.Parse("network.timeoutSeconds");
        Assert.True(pointer.TryEvaluate(node, out var a));
        Assert.True(dotted.TryEvaluate(node, out var b));
        Assert.Equal(15, a!.GetValue<int>());
        Assert.Equal(15, b!.GetValue<int>());

        Assert.True(JsonPath.Parse("/records/0/code").TryEvaluate(node, out var c));
        Assert.True(JsonPath.Parse("records[0].code").TryEvaluate(node, out var d));
        Assert.Equal("ok", c!.GetValue<string>());
        Assert.Equal("ok", d!.GetValue<string>());
    }

    [Fact]
    public void Jsonl_index_path_reads_first_array_element()
    {
        var node = JsonHelper.Parse("""[{"code":"alpha"},{"code":"beta"}]""");
        Assert.True(JsonPath.Parse("[0].code").TryEvaluate(node, out var first));
        Assert.Equal("alpha", first!.GetValue<string>());
        Assert.True(JsonPath.Parse("/1/code").TryEvaluate(node, out var second));
        Assert.Equal("beta", second!.GetValue<string>());
    }

    [Fact]
    public void Pointer_escapes_slash_and_tilde()
    {
        var node = JsonHelper.Parse("""{"a/b":1,"m~n":2}""");
        Assert.True(JsonPath.Parse("/a~1b").TryEvaluate(node, out var slash));
        Assert.Equal(1, slash!.GetValue<int>());
        Assert.True(JsonPath.Parse("/m~0n").TryEvaluate(node, out var tilde));
        Assert.Equal(2, tilde!.GetValue<int>());
    }

    [Fact]
    public void Invalid_path_throws()
    {
        Assert.Throws<ArgumentException>(() => JsonPath.Parse("records[*].code"));
        Assert.Throws<ArgumentException>(() => JsonPath.Parse("$..id"));
        Assert.Throws<ArgumentException>(() => JsonPath.Parse("records[01].code"));
        Assert.Throws<ArgumentNullException>(() => JsonPath.Parse(null));
        Assert.Throws<ArgumentException>(() => JsonPath.Parse("   "));
    }

    [Fact]
    public void Missing_member_is_not_found()
    {
        var node = JsonHelper.Parse("""{"a":1}""");
        Assert.False(JsonPath.Parse("/b").TryEvaluate(node, out _));
        Assert.False(JsonPath.Parse("a[0]").TryEvaluate(node, out _));
    }

    [Fact]
    public void Parse_stream_round_trips_without_read_all_bytes()
    {
        var json = JsonHelper.ToJson(new ProbeDto { TimeoutSeconds = 9, Level = "Debug" }, new JsonWriteOptions { WriteIndented = false });
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
        var node = JsonHelper.Parse(stream);
        Assert.Equal(9, node["timeoutSeconds"]!.GetValue<int>());
    }

    [Fact]
    public void Logs_do_not_contain_payload_bodies()
    {
        var dir = Path.Combine(Path.GetTempPath(), "VestigiumJsonTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        HelperLog.InitializeHost(HelperLog.AppIds.Json, cfg => cfg.LogDirectory = dir);
        try
        {
            const string secret = "hunter2-payload-must-not-log";
            _ = JsonHelper.ToJson(new ProbeDto { TimeoutSeconds = 1, Level = secret });
            Assert.DoesNotContain(HelperLog.RecentJsonLines, l => l.Contains(secret));
            Assert.Contains(HelperLog.RecentJsonLines, l => l.Contains("ToJson") && l.Contains("bytes="));
            Assert.StartsWith(Path.GetTempPath().TrimEnd(Path.DirectorySeparatorChar), dir);
        }
        finally
        {
            HelperLog.Shutdown();
            Directory.Delete(dir, recursive: true);
        }
    }

    private sealed class ProbeDto
    {
        public int TimeoutSeconds { get; set; }
        public string Level { get; set; } = "";
    }
}
