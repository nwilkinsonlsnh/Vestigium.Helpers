using Vestigium.Helpers.Processes;

namespace Vestigium.Helpers.Tests;

[Collection("Logger")]
public sealed class ProcessCampaignTests
{
    [Fact]
    public void CreateCampaign_writes_recipe_and_samples_inside_window()
    {
        var root = Path.Combine(Path.GetTempPath(), "VestigiumProcessCampaigns", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        ProcessTestHooks.CampaignRoot = root;
        var now = new DateTimeOffset(2026, 9, 10, 8, 1, 0, TimeSpan.FromHours(-4));
        ProcessTestHooks.Now = () => now;
        try
        {
            var self = ProcessHelper.Get(Environment.ProcessId, ProcessDetailLevel.Identity);
            Assert.NotNull(self);
            var campaign = ProcessHelper.CreateCampaign(new ProcessCampaignRecipe
            {
                Name = "day-parts",
                Match = new ProcessSearchRequest { Term = self.Name, Mode = ProcessSearchMode.EndsWith },
                SampleInterval = TimeSpan.FromMilliseconds(250),
                IncludeSystemCounters = true,
                Windows =
                [
                    new ProcessCampaignWindow("morning", new TimeOnly(8, 0), TimeSpan.FromMinutes(10), ProcessCampaignDays.All)
                ]
            });

            Assert.True(File.Exists(campaign.RecipePath));
            Assert.Contains("day-parts", ProcessHelper.ListCampaigns());
            campaign.TickOnce();
            Assert.Equal(ProcessCampaignState.Sampling, campaign.State);
            Assert.True(File.Exists(campaign.SamplePath));
            var text = File.ReadAllText(campaign.SamplePath);
            Assert.Contains("\"kind\":\"process\"", text);
            Assert.Contains("\"kind\":\"system\"", text);
            Assert.Contains(self.Name, text);
        }
        finally
        {
            ProcessTestHooks.CampaignRoot = null;
            ProcessTestHooks.Now = null;
            try { Directory.Delete(root, true); } catch { }
        }
    }

    [Fact]
    public void Tick_outside_window_is_waiting_and_writes_nothing()
    {
        var root = Path.Combine(Path.GetTempPath(), "VestigiumProcessCampaigns", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        ProcessTestHooks.CampaignRoot = root;
        ProcessTestHooks.Now = () => new DateTimeOffset(2026, 9, 10, 3, 0, 0, TimeSpan.FromHours(-4));
        try
        {
            var campaign = ProcessHelper.CreateCampaign(new ProcessCampaignRecipe
            {
                Name = "noon-only",
                Match = new ProcessSearchRequest { Term = "testhost", Mode = ProcessSearchMode.Contains },
                SampleInterval = TimeSpan.FromMilliseconds(250),
                IncludeSystemCounters = false,
                Windows =
                [
                    new ProcessCampaignWindow("noon", new TimeOnly(12, 0), TimeSpan.FromMinutes(10), ProcessCampaignDays.All)
                ]
            });
            campaign.TickOnce();
            Assert.Equal(ProcessCampaignState.Waiting, campaign.State);
            Assert.False(File.Exists(campaign.SamplePath));
        }
        finally
        {
            ProcessTestHooks.CampaignRoot = null;
            ProcessTestHooks.Now = null;
            try { Directory.Delete(root, true); } catch { }
        }
    }

    [Fact]
    public void CreateCampaign_rejects_empty_windows()
    {
        Assert.Throws<ArgumentException>(() => ProcessHelper.CreateCampaign(new ProcessCampaignRecipe
        {
            Name = "empty",
            Match = new ProcessSearchRequest { Term = "x", Mode = ProcessSearchMode.Contains },
            Windows = []
        }));
    }
}
