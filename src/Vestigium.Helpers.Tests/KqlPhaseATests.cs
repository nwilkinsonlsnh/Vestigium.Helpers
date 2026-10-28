using Vestigium.Helpers.Kql;

namespace Vestigium.Helpers.Tests;

[Collection("Logger")]
public sealed class KqlPhaseATests
{
    [Fact]
    public void Process_pack_exposes_description_and_signer()
    {
        using var session = KqlHelper.Create(KqlPack.Process);
        Assert.True(session.TryGetField("Description", out var description));
        Assert.Equal("PROC.Description", description.Canonical);
        Assert.True(session.TryGetField("Signer", out var signer));
        Assert.Equal("PROC.Signer", signer.Canonical);
        Assert.True(session.TryGetField("PROC.Aslr", out var aslr));
        Assert.Equal(KqlType.Boolean, aslr.Type);
        Assert.True(session.TryGetField("Comment", out _));
        Assert.True(session.TryGetField("Package", out _));
    }

    [Fact]
    public void Service_pack_rejects_process_description()
    {
        using var session = KqlHelper.Create(KqlPack.Service);
        Assert.False(session.TryGetField("PROC.Description", out _));
        Assert.False(session.TryGetField("Description", out _));
        var compiled = KqlHelper.Compile("Description == 'x'", session);
        Assert.False(compiled.Ok);
        Assert.Contains("pack=Service", compiled.Error!.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Bind_error_names_pack_and_caps_enabled_list()
    {
        using var session = KqlHelper.Create(KqlPack.Process);
        var compiled = KqlHelper.Compile("Nope == 1", session);
        Assert.False(compiled.Ok);
        Assert.Contains("unknown field 'Nope'", compiled.Error!.Message, StringComparison.Ordinal);
        Assert.Contains("pack=Process", compiled.Error.Message, StringComparison.Ordinal);
        Assert.Contains("enabled (", compiled.Error.Message, StringComparison.Ordinal);
        Assert.Contains("+", compiled.Error.Message, StringComparison.Ordinal);
        Assert.True(compiled.Error.Message.Length < 800, compiled.Error.Message);
    }

    [Fact]
    public void Cpu_usage_is_watch_only()
    {
        using var session = KqlHelper.Create(KqlPack.Process);
        Assert.True(session.TryGetField("CPU.Usage", out var usage));
        Assert.True(usage.WatchOnly);
        Assert.True(session.TryGetField("IO.ReadBytesDelta", out var delta));
        Assert.True(delta.WatchOnly);
        Assert.True(session.TryGetField("PROC.Name", out var name));
        Assert.False(name.WatchOnly);
    }

    [Fact]
    public void Empty_string_is_unknown()
    {
        var value = KqlValue.From("   ");
        Assert.True(value.IsUnknown);
        Assert.True(KqlValue.From("").IsUnknown);
        Assert.False(KqlValue.From("msedge").IsUnknown);
    }
}
