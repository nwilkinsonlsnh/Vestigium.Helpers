using Vestigium.Helpers.Json;
using Vestigium.Helpers.Network;

namespace Vestigium.Helpers.Tests;

public sealed class IcmpEchoCampaignTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "VestigiumNetworkCampaigns", Guid.NewGuid().ToString("N"));

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

    [Fact]
    public void Recipe_round_trips_echo_options()
    {
        var recipe = Path.Combine(_root, "echo-recipe.json");
        var created = NetworkHelper.CreateEchoCampaign(new IcmpEchoCampaignOptions
        {
            Target = "127.0.0.1",
            RangeStartDate = new DateOnly(2026, 9, 9),
            RangeEndDate = new DateOnly(2026, 9, 30),
            RecipePath = recipe,
            Windows = [new EchoWindow(new TimeOnly(12, 0), 3)],
            Echo = new IcmpEchoOptions
            {
                Count = 99,
                Timeout = TimeSpan.FromMilliseconds(1500),
                Interval = TimeSpan.FromMilliseconds(250),
                BufferSize = 64,
                Ttl = 32,
                DontFragment = true,
                MaxDuration = TimeSpan.FromSeconds(5),
                AllowBurst = true
            }
        });

        var text = File.ReadAllText(recipe);
        Assert.Contains("\"timeoutMs\": 1500", text, StringComparison.Ordinal);
        Assert.Contains("\"intervalMs\": 250", text, StringComparison.Ordinal);
        Assert.Contains("\"bufferSize\": 64", text, StringComparison.Ordinal);
        Assert.Contains("\"ttl\": 32", text, StringComparison.Ordinal);
        Assert.Contains("\"dontFragment\": true", text, StringComparison.Ordinal);
        Assert.Contains("\"maxDurationMs\": 5000", text, StringComparison.Ordinal);
        Assert.Contains("\"allowBurst\": true", text, StringComparison.Ordinal);
        Assert.DoesNotContain("\"count\": 99", text, StringComparison.Ordinal);

        var opened = NetworkHelper.OpenEchoCampaign(recipe);
        Assert.Equal(created.CampaignId, opened.CampaignId);
        Assert.Equal(TimeSpan.FromMilliseconds(1500), opened.Options.Echo.Timeout);
        Assert.Equal(TimeSpan.FromMilliseconds(250), opened.Options.Echo.Interval);
        Assert.Equal(64, opened.Options.Echo.BufferSize);
        Assert.Equal(32, opened.Options.Echo.Ttl);
        Assert.True(opened.Options.Echo.DontFragment);
        Assert.Equal(TimeSpan.FromSeconds(5), opened.Options.Echo.MaxDuration);
        Assert.True(opened.Options.Echo.AllowBurst);
        Assert.Equal(IcmpEchoOptions.DefaultCount, opened.Options.Echo.Count);
        Assert.Equal(3, opened.Options.Windows[0].Count);
    }

    [Fact]
    public void Old_recipe_without_echo_opens_with_defaults()
    {
        var recipe = Path.Combine(_root, "old-recipe.json");
        File.WriteAllText(recipe, """
            {
              "campaignId": "camp-old",
              "target": "127.0.0.1",
              "rangeStartDate": "2026-09-09",
              "rangeEndDate": "2026-09-30",
              "graceMinutes": 15,
              "windows": [ { "localTime": "12:00", "count": 2 } ]
            }
            """);

        var opened = NetworkHelper.OpenEchoCampaign(recipe);
        Assert.Equal("camp-old", opened.CampaignId);
        Assert.Equal(IcmpEchoOptions.DefaultTimeout, opened.Options.Echo.Timeout);
        Assert.Equal(IcmpEchoOptions.DefaultInterval, opened.Options.Echo.Interval);
        Assert.Equal(IcmpEchoOptions.DefaultBufferSize, opened.Options.Echo.BufferSize);
        Assert.Equal(128, opened.Options.Echo.Ttl);
        Assert.False(opened.Options.Echo.DontFragment);
        Assert.Null(opened.Options.Echo.MaxDuration);
        Assert.False(opened.Options.Echo.AllowBurst);
        Assert.Equal(2, opened.Options.Windows[0].Count);
    }
}
