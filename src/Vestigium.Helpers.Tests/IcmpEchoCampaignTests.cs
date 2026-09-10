using Vestigium.Helpers.Json;
using Vestigium.Helpers.Network;

namespace Vestigium.Helpers.Tests;

public sealed class IcmpEchoCampaignTests : IDisposable
{
    readonly string _root = Path.Combine(Path.GetTempPath(), "VestigiumNetworkCampaigns", Guid.NewGuid().ToString("N"));

    public IcmpEchoCampaignTests()
    {
        Directory.CreateDirectory(_root);
        NetworkTestHooks.CampaignRoot = _root;
        NetworkTestHooks.UtcNow = new DateTimeOffset(2026, 9, 10, 12, 5, 0, TimeSpan.Zero);
    }

    public void Dispose()
    {
        NetworkTestHooks.Reset();
        try { Directory.Delete(_root, true); } catch { }
    }

    [Fact]
    public async Task In_grace_window_runs_once_and_is_idempotent()
    {
        var results = Path.Combine(_root, "run.jsonl");
        var campaign = NetworkHelper.CreateEchoCampaign(new IcmpEchoCampaignOptions
        {
            Target = "127.0.0.1",
            RangeStartDate = new DateOnly(2026, 9, 9),
            RangeEndDate = new DateOnly(2026, 9, 30),
            TimeZoneId = "UTC",
            ResultsPath = results,
            Grace = TimeSpan.FromMinutes(15),
            Echo = new IcmpEchoOptions { Timeout = TimeSpan.FromMilliseconds(400), Interval = TimeSpan.Zero },
            Windows =
            [
                new EchoWindow(new TimeOnly(12, 0), 1),
                new EchoWindow(new TimeOnly(14, 0), 1)
            ]
        });

        var first = await campaign.RunAsync();
        var second = await campaign.RunAsync();

        Assert.Equal(1, first.WindowsRun);
        Assert.Equal(0, second.WindowsRun);
        Assert.True(second.WindowsSkipped >= 1);

        using var session = JsonHelper.OpenJsonl(results);
        var kinds = new List<string>();
        for (var i = 0; i < session.RecordCount; i++)
        {
            var node = session.Record(i);
            if (node?["kind"] is { } kind)
                kinds.Add(kind.GetValue<string>());
        }

        Assert.Contains("echo", kinds);
        Assert.Contains("windowSummary", kinds);
        Assert.Equal(1, kinds.Count(k => k == "windowSummary"));
    }

    [Fact]
    public async Task Past_grace_writes_windowMissed_without_echoes()
    {
        NetworkTestHooks.UtcNow = new DateTimeOffset(2026, 9, 10, 12, 20, 0, TimeSpan.Zero);
        var results = Path.Combine(_root, "missed.jsonl");
        var campaign = NetworkHelper.CreateEchoCampaign(new IcmpEchoCampaignOptions
        {
            Target = "127.0.0.1",
            RangeStartDate = new DateOnly(2026, 9, 9),
            RangeEndDate = new DateOnly(2026, 9, 30),
            TimeZoneId = "UTC",
            ResultsPath = results,
            Grace = TimeSpan.FromMinutes(15),
            Windows = [new EchoWindow(new TimeOnly(12, 0), 1)]
        });

        var result = await campaign.RunAsync();
        Assert.Equal(1, result.WindowsMissed);
        Assert.Equal(0, result.WindowsRun);
        Assert.Equal(0, result.EchoesAppended);

        using var session = JsonHelper.OpenJsonl(results);
        var kinds = new List<string>();
        for (var i = 0; i < session.RecordCount; i++)
        {
            var node = session.Record(i);
            if (node?["kind"] is { } kind)
                kinds.Add(kind.GetValue<string>());
        }

        Assert.Contains("windowMissed", kinds);
        Assert.DoesNotContain("echo", kinds);
    }
}
