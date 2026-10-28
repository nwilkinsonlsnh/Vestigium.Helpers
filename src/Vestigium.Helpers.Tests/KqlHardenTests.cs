using Vestigium.Helpers;
using Vestigium.Helpers.Kql;
using Vestigium.Logging;

namespace Vestigium.Helpers.Tests;

[Collection("Logger")]
public sealed class KqlHardenTests
{
    [Fact]
    public void Kql_project_does_not_reference_processes()
    {
        var path = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..", "..",
            "Vestigium.Helpers.Kql",
            "Vestigium.Helpers.Kql.csproj"));
        Assert.True(File.Exists(path), path);
        var text = File.ReadAllText(path);
        Assert.DoesNotContain("Vestigium.Helpers.Processes", text, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Exact_warning_does_not_log_rhs_string()
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
            Assert.DoesNotContain(
                HelperLog.RecentJsonLines,
                line => line.Contains("CCleaner", StringComparison.OrdinalIgnoreCase));
        }
        finally
        {
            HelperLog.Shutdown();
        }
    }

    [Fact]
    public void Unknown_or_true_is_true()
    {
        using var session = KqlHelper.Create(KqlPack.Process);
        var compiled = KqlHelper.Compile("GPU.Usage GT 20 || PID == 10", session);
        Assert.True(compiled.Ok, compiled.Error?.ToString());
        var row = new KqlFixtureRow(session).Set("PID", 10);
        Assert.Equal(KqlTriState.True, compiled.Query!.Evaluate(row));
        Assert.True(compiled.Query.Matches(row));
    }

    [Fact]
    public void Type_mismatch_is_compile_error()
    {
        using var session = KqlHelper.Create(KqlPack.Process);
        var compiled = KqlHelper.Compile("PID LIKE '%10%'", session);
        Assert.False(compiled.Ok);
        Assert.Contains("LIKE", compiled.Error!.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Double_quoted_string_parses()
    {
        var parsed = KqlHelper.Parse("Name == \"msedge\"");
        Assert.True(parsed.Ok, parsed.Error?.ToString());
        var cmp = Assert.IsType<KqlComparisonExpression>(parsed.Expression);
        Assert.Equal("msedge", cmp.Value.Value);
    }

    [Fact]
    public void Guide_example_compiles_on_process_pack()
    {
        using var session = KqlHelper.Create(KqlPack.Process);
        var compiled = KqlHelper.Compile(
            "(PID == 45944 || Name LIKE 'CCleaner%') && GPU.Usage GT 20",
            session);
        Assert.True(compiled.Ok, compiled.Error?.ToString());
    }
}
