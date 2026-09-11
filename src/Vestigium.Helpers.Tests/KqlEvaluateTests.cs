using Vestigium.Helpers.Kql;

namespace Vestigium.Helpers.Tests;

[Collection("Logger")]
public sealed class KqlEvaluateTests
{
    [Fact]
    public void Process_row_matches_name_and_pid()
    {
        using var session = KqlHelper.Create(KqlPack.Process);
        var compiled = KqlHelper.Compile("Name LIKE '%edge%' && PID == 10", session);
        Assert.True(compiled.Ok, compiled.Error?.ToString());
        var row = new KqlRow(session).Set("Name", "msedge").Set("PID", 10);
        Assert.True(compiled.Query!.Matches(row));
    }

    [Fact]
    public void Missing_gpu_is_unknown_and_not_a_hit()
    {
        using var session = KqlHelper.Create(KqlPack.Process);
        var compiled = KqlHelper.Compile("GPU.Usage GT 20", session);
        Assert.True(compiled.Ok, compiled.Error?.ToString());
        var row = new KqlRow(session).Set("Name", "msedge").Set("PID", 10);
        Assert.Equal(KqlTriState.Unknown, compiled.Query!.Evaluate(row));
        Assert.False(compiled.Query.Matches(row));
    }

    [Fact]
    public void Service_session_rejects_mem_private_bytes()
    {
        using var session = KqlHelper.Create(KqlPack.Service);
        var compiled = KqlHelper.Compile("MEM.PrivateBytes > 1", session);
        Assert.False(compiled.Ok);
        Assert.Contains("unknown field", compiled.Error!.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("SVC.Name", compiled.Error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Unknown_and_false_is_false()
    {
        using var session = KqlHelper.Create(KqlPack.Process);
        var compiled = KqlHelper.Compile("GPU.Usage GT 20 && PID == 11", session);
        Assert.True(compiled.Ok, compiled.Error?.ToString());
        var row = new KqlRow(session).Set("PID", 10);
        Assert.Equal(KqlTriState.False, compiled.Query!.Evaluate(row));
    }
}
