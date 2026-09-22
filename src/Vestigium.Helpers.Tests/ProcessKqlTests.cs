using Vestigium.Helpers.Kql;
using Vestigium.Helpers.Processes;

namespace Vestigium.Helpers.Tests;

[Collection("Logger")]
public sealed class ProcessKqlTests
{
    [Fact]
    public void Search_query_by_pid_returns_host()
    {
        var hits = ProcessHelper.Search("PID == " + Environment.ProcessId);
        Assert.Contains(hits, row => row.Pid == Environment.ProcessId);
    }

    [Fact]
    public void Search_query_like_testhost_returns_host()
    {
        var self = ProcessHelper.Get(Environment.ProcessId, ProcessDetailLevel.Identity);
        Assert.NotNull(self);
        var hits = ProcessHelper.Search("Name LIKE '%" + Path.GetFileNameWithoutExtension(self.Name) + "%'");
        Assert.Contains(hits, row => row.Pid == Environment.ProcessId);
    }

    [Fact]
    public void Description_like_matches_bound_row()
    {
        using var session = KqlHelper.Create(KqlPack.Process);
        var compiled = KqlHelper.Compile("Description LIKE '%'", session);
        Assert.True(compiled.Ok, compiled.Error?.ToString());
        var row = new ProcessKqlRow(new ProcessInfo { Pid = 1, Name = "app.exe", Description = "Widget Host" });
        Assert.True(compiled.Query!.Matches(row));
    }

    [Fact]
    public void Missing_window_title_does_not_match_empty_string()
    {
        using var session = KqlHelper.Create(KqlPack.Process);
        var compiled = KqlHelper.Compile("WindowTitle == ''", session);
        Assert.True(compiled.Ok, compiled.Error?.ToString());
        var row = new ProcessKqlRow(new ProcessInfo { Pid = 1, Name = "app.exe", WindowTitle = null });
        Assert.False(compiled.Query!.Matches(row));
        Assert.Equal(KqlTriState.Unknown, compiled.Query.Evaluate(row));
    }

    [Fact]
    public void Command_line_query_upgrades_to_full()
    {
        using var session = KqlHelper.Create(KqlPack.Process);
        var parsed = KqlHelper.Parse("CommandLine LIKE '%dotnet%'");
        Assert.True(parsed.Ok, parsed.Error?.ToString());
        Assert.True(ProcessKqlLevel.NeedsFull(parsed.Expression!, session));
        Assert.False(ProcessKqlLevel.NeedsFull(KqlHelper.Parse("PID == 1").Expression!, session));
    }

    [Fact]
    public void Search_command_line_query_does_not_throw()
    {
        var hits = ProcessHelper.Search("CommandLine LIKE '%dotnet%'", ProcessDetailLevel.Slim, 64);
        Assert.NotNull(hits);
    }

    [Fact]
    public void Campaign_query_writes_jsonl()
    {
        var root = Path.Combine(Path.GetTempPath(), "VestigiumProcessCampaigns", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        ProcessTestHooks.CampaignRoot = root;
        var now = new DateTimeOffset(2026, 9, 10, 8, 1, 0, TimeSpan.FromHours(-4));
        ProcessTestHooks.Now = () => now;
        try
        {
            var campaign = ProcessHelper.CreateCampaign(new ProcessCampaignRecipe
            {
                Name = "kql-window",
                Match = new ProcessSearchRequest { Term = "unused", Mode = ProcessSearchMode.Contains },
                Query = "PID == " + Environment.ProcessId,
                SampleInterval = TimeSpan.FromMilliseconds(250),
                IncludeSystemCounters = false,
                Windows =
                [
                    new ProcessCampaignWindow("morning", new TimeOnly(8, 0), TimeSpan.FromMinutes(10), ProcessCampaignDays.All)
                ]
            });

            campaign.TickOnce();
            Assert.True(File.Exists(campaign.SamplePath));
            var text = File.ReadAllText(campaign.SamplePath);
            Assert.Contains("\"kind\":\"process\"", text);
            Assert.Contains(Environment.ProcessId.ToString(), text);
        }
        finally
        {
            ProcessTestHooks.CampaignRoot = null;
            ProcessTestHooks.Now = null;
            try { Directory.Delete(root, true); } catch { }
        }
    }
}
