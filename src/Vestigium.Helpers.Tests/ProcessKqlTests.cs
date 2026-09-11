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
