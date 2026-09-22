using Vestigium.Helpers.Kql;
using Vestigium.Helpers.Processes;

namespace Vestigium.Helpers.Tests;

[Collection("Logger")]
public sealed class KqlBacklogB5Tests
{
    [Fact]
    public void Exact_equals_with_wildcard_returns_diagnostic()
    {
        using var session = KqlHelper.Create(KqlPack.Process);
        var compiled = KqlHelper.Compile("Name == 'CCleaner%'", session);
        Assert.True(compiled.Ok);
        Assert.Contains(compiled.Diagnostics, line => line.Contains("wildcard", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Short_names_bind_on_process_pack()
    {
        using var session = KqlHelper.Create(KqlPack.Process);
        Assert.True(KqlHelper.Compile("WindowTitle LIKE '%'", session).Ok);
        Assert.True(KqlHelper.Compile("CommandLine LIKE '%'", session).Ok);
        Assert.True(KqlHelper.Compile("Name == 'x'", session).Ok);
        var self = ProcessHelper.Search("CommandLine LIKE '%' || WindowTitle LIKE '%'", ProcessDetailLevel.Full, 8);
        Assert.NotNull(self);
    }
}
