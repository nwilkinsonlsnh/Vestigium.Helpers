using System.Text;
using System.Text.Json;
using Vestigium.Helpers.Json;

namespace Vestigium.Helpers.Tests;

[Collection("Logger")]
public sealed class JsonSpanParseTests
{
    [Fact]
    public void Parse_span_reads_an_object()
    {
        ReadOnlySpan<byte> utf8 = "{\"appId\":\"PingIQ\"}"u8;
        var node = JsonHelper.Parse(utf8);
        Assert.Equal("PingIQ", node["appId"]!.GetValue<string>());
    }

    [Fact]
    public void Parse_span_rejects_empty()
    {
        var ex = Assert.Throws<ArgumentException>(() => JsonHelper.Parse(ReadOnlySpan<byte>.Empty));
        Assert.Equal("utf8Json", ex.ParamName);
    }

    [Fact]
    public void Parse_span_rejects_bom()
    {
        var payload = Encoding.UTF8.GetBytes("{\"a\":1}");
        var bom = new byte[] { 0xEF, 0xBB, 0xBF }.Concat(payload).ToArray();
        var ex = Assert.Throws<JsonException>(() => JsonHelper.Parse(bom));
        Assert.Contains("BOM", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Parse_span_rejects_json_null()
    {
        var ex = Assert.Throws<JsonException>(() => JsonHelper.Parse("null"u8));
        Assert.Contains("JSON null", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Parse_span_rejects_truncated_object()
        => Assert.ThrowsAny<JsonException>(() => JsonHelper.Parse("{"u8));
}
