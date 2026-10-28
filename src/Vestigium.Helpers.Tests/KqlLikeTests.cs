using Vestigium.Helpers;
using Vestigium.Helpers.Kql;
using Vestigium.Logging;

namespace Vestigium.Helpers.Tests;

[Collection("Logger")]
public sealed class KqlLikeTests
{
    [Fact]
    public void Like_prefix_matches_ccleaner()
    {
        using var session = KqlHelper.Create(KqlPack.Process);
        var compiled = KqlHelper.Compile("Name LIKE 'CCleaner%'", session);
        Assert.True(compiled.Ok, compiled.Error?.ToString());
        var row = new KqlFixtureRow(session).Set("Name", "CCleaner64.exe");
        Assert.True(compiled.Query!.Matches(row));
    }

    [Fact]
    public void Exact_equals_treats_percent_as_literal()
    {
        using var session = KqlHelper.Create(KqlPack.Process);
        var compiled = KqlHelper.Compile("Name == 'CCleaner%'", session);
        Assert.True(compiled.Ok, compiled.Error?.ToString());
        var miss = new KqlFixtureRow(session).Set("Name", "CCleaner64.exe");
        var hit = new KqlFixtureRow(session).Set("Name", "CCleaner%");
        Assert.False(compiled.Query!.Matches(miss));
        Assert.True(compiled.Query.Matches(hit));
    }

    [Fact]
    public void Like_contains_edge_is_case_insensitive()
    {
        using var session = KqlHelper.Create(KqlPack.Process);
        var compiled = KqlHelper.Compile("Name LIKE '%EDGE%'", session);
        Assert.True(compiled.Ok, compiled.Error?.ToString());
        Assert.True(compiled.Query!.Matches(new KqlFixtureRow(session).Set("Name", "msedge")));
    }

    [Fact]
    public void Like_question_matches_one_character()
    {
        using var session = KqlHelper.Create(KqlPack.Process);
        var compiled = KqlHelper.Compile("Name LIKE 'ms?'", session);
        Assert.True(compiled.Ok, compiled.Error?.ToString());
        Assert.True(compiled.Query!.Matches(new KqlFixtureRow(session).Set("Name", "ms1")));
        Assert.False(compiled.Query.Matches(new KqlFixtureRow(session).Set("Name", "msedge")));
    }

    [Fact]
    public void Exact_equals_with_wildcard_logs_warning_and_still_compiles()
    {
        HelperLog.Shutdown();
        var dir = Path.Combine(Path.GetTempPath(), "VestigiumHelpersTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        HelperLog.InitializeHost(HelperLog.AppIds.Kql, cfg => cfg.LogDirectory = dir);
        try
        {
            using var session = KqlHelper.Create(KqlPack.Process);
            var compiled = KqlHelper.Compile("Name == 'CCleaner%'", session);
            Assert.True(compiled.Ok, compiled.Error?.ToString());
            var lines = HelperLog.RecentJsonLines;
            Assert.Contains(
                lines,
                line => line.Contains("exact compare treats wildcard chars as literals", StringComparison.Ordinal)
                    && line.Contains("chars=%", StringComparison.Ordinal)
                    && line.Contains("field=PROC.Name", StringComparison.Ordinal));
        }
        finally
        {
            HelperLog.Shutdown();
        }
    }

    [Fact]
    public void Exact_equals_without_wildcard_does_not_warn()
    {
        HelperLog.Shutdown();
        var dir = Path.Combine(Path.GetTempPath(), "VestigiumHelpersTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        HelperLog.InitializeHost(HelperLog.AppIds.Kql, cfg => cfg.LogDirectory = dir);
        try
        {
            using var session = KqlHelper.Create(KqlPack.Process);
            var compiled = KqlHelper.Compile("Name == 'msedge'", session);
            Assert.True(compiled.Ok, compiled.Error?.ToString());
            Assert.DoesNotContain(
                HelperLog.RecentJsonLines,
                line => line.Contains("exact compare treats wildcard chars as literals", StringComparison.Ordinal));
        }
        finally
        {
            HelperLog.Shutdown();
        }
    }
}
