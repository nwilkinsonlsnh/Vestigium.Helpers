using Vestigium.Helpers.Kql;

namespace Vestigium.Helpers.Tests;

[Collection("Logger")]
public sealed class KqlPhaseGTests
{
    [Fact]
    public void Name_in_list_matches()
    {
        using var session = KqlHelper.Create(KqlPack.Process);
        var compiled = KqlHelper.Compile("Name IN ('a','b')", session);
        Assert.True(compiled.Ok, compiled.Error?.ToString());
        var row = new KqlRow(session).Set("Name", "b");
        Assert.True(compiled.Query!.Matches(row));
    }

    [Fact]
    public void Private_bytes_between_compiles_and_matches()
    {
        using var session = KqlHelper.Create(KqlPack.Process);
        var compiled = KqlHelper.Compile("MEM.PrivateBytes BETWEEN 1 AND 3", session);
        Assert.True(compiled.Ok, compiled.Error?.ToString());
        var row = new KqlRow(session).Set("MEM.PrivateBytes", 2L);
        Assert.True(compiled.Query!.Matches(row));
        row.Set("MEM.PrivateBytes", 9L);
        Assert.False(compiled.Query.Matches(row));
    }

    [Fact]
    public void Type_mismatch_names_types_not_rhs_text()
    {
        using var session = KqlHelper.Create(KqlPack.Process);
        var compiled = KqlHelper.Compile("PID == 'secret-token'", session);
        Assert.False(compiled.Ok);
        Assert.Contains("field=PROC.Pid", compiled.Error!.Message, StringComparison.Ordinal);
        Assert.Contains("type=Integer", compiled.Error.Message, StringComparison.Ordinal);
        Assert.Contains("rhs=String", compiled.Error.Message, StringComparison.Ordinal);
        Assert.DoesNotContain("secret-token", compiled.Error.Message, StringComparison.Ordinal);
    }
}
