using Vestigium.Helpers.Services;

namespace Vestigium.Helpers.Tests;

[Collection("Logger")]
public sealed class ServicePhase7Tests : IDisposable
{
    public ServicePhase7Tests()
    {
        ServiceTestHooks.CampaignRoot = Path.Combine(Path.GetTempPath(), "vest-svc-" + Guid.NewGuid().ToString("N"));
        ServiceTestHooks.Now = () => new DateTimeOffset(2026, 9, 12, 12, 0, 0, TimeSpan.Zero);
    }

    public void Dispose()
    {
        var root = ServiceTestHooks.CampaignRoot;
        ServiceTestHooks.CampaignRoot = null;
        ServiceTestHooks.Now = null;
        if (root is not null && Directory.Exists(root))
        {
            try { Directory.Delete(root, recursive: true); } catch { }
        }
    }

    [Fact]
    public void Watch_interval_out_of_range_throws()
        => Assert.Throws<ArgumentOutOfRangeException>(() => ServiceHelper.Watch("EventLog", TimeSpan.FromMilliseconds(10)));

    [Fact]
    public void Watch_eventlog_samples()
    {
        using var watch = ServiceHelper.Watch("EventLog", TimeSpan.FromMilliseconds(250));
        IReadOnlyList<ServiceInfo>? sample = null;
        watch.Sampled += (_, rows) => sample = rows;
        var until = DateTime.UtcNow.AddSeconds(3);
        while (sample is null && DateTime.UtcNow < until)
            Thread.Sleep(50);
        Assert.NotNull(sample);
        Assert.Contains(sample!, row => string.Equals(row.Name, "EventLog", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Search_kql_eventlog_by_name()
    {
        var hits = ServiceHelper.Search("Name LIKE 'Event%'");
        Assert.Contains(hits, row => string.Equals(row.Name, "EventLog", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Search_kql_bad_field_throws()
        => Assert.Throws<ArgumentException>(() => ServiceHelper.Search("PID == 1"));

    [Fact]
    public void Campaign_writes_jsonl_inside_window()
    {
        var campaign = ServiceHelper.CreateCampaign(new ServiceCampaignRecipe
        {
            Name = "live-demo",
            Query = "Name LIKE 'Event%'",
            SampleInterval = TimeSpan.FromSeconds(1),
            TimeZoneId = "UTC",
            Windows =
            [
                new ServiceCampaignWindow("noon", new TimeOnly(12, 0), TimeSpan.FromMinutes(10), ServiceCampaignDays.All)
            ]
        });

        campaign.TickOnce();
        Assert.Equal(ServiceCampaignState.Sampling, campaign.State);
        Assert.True(File.Exists(campaign.SamplePath));
        var text = File.ReadAllText(campaign.SamplePath);
        Assert.Contains("EventLog", text, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("\"kind\":\"service\"", text);
        Assert.DoesNotContain("password", text, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Campaign_outside_window_does_not_sample()
    {
        ServiceTestHooks.Now = () => new DateTimeOffset(2026, 9, 12, 3, 0, 0, TimeSpan.Zero);
        var campaign = ServiceHelper.CreateCampaign(new ServiceCampaignRecipe
        {
            Name = "off-hours",
            Match = new ServiceSearchRequest { Term = "Event", Mode = ServiceSearchMode.StartsWith },
            SampleInterval = TimeSpan.FromSeconds(1),
            TimeZoneId = "UTC",
            Windows =
            [
                new ServiceCampaignWindow("noon", new TimeOnly(12, 0), TimeSpan.FromMinutes(5), ServiceCampaignDays.All)
            ]
        });
        campaign.TickOnce();
        Assert.Equal(ServiceCampaignState.Waiting, campaign.State);
        Assert.False(File.Exists(campaign.SamplePath));
    }

    [Fact]
    public void Campaign_needs_window_and_filter()
    {
        Assert.Throws<ArgumentException>(() => ServiceHelper.CreateCampaign(new ServiceCampaignRecipe
        {
            Name = "empty",
            SampleInterval = TimeSpan.FromSeconds(1),
            Windows = [new ServiceCampaignWindow("noon", new TimeOnly(12, 0), TimeSpan.FromMinutes(5), ServiceCampaignDays.All)]
        }));
    }

    [Fact]
    public void ListCampaigns_sees_created()
    {
        ServiceHelper.CreateCampaign(new ServiceCampaignRecipe
        {
            Name = "listed",
            Query = "Name LIKE 'Event%'",
            SampleInterval = TimeSpan.FromSeconds(1),
            TimeZoneId = "UTC",
            Windows = [new ServiceCampaignWindow("noon", new TimeOnly(12, 0), TimeSpan.FromMinutes(5), ServiceCampaignDays.All)]
        });
        Assert.Contains("listed", ServiceHelper.ListCampaigns());
    }
}
