using Vestigium.Helpers.Network;

namespace Vestigium.Helpers.Tests;

public sealed class EchoWindowDurationTests : IDisposable
{
    private readonly string _root;

    public EchoWindowDurationTests()
    {
        _root = Path.Combine(Path.GetTempPath(), "vest-echo-window-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_root);
        CampaignPaths.OverrideRoot(_root);
    }

    public void Dispose()
    {
        CampaignPaths.OverrideRoot(null);
        try { Directory.Delete(_root, true); } catch (IOException) { }
    }

    [Fact]
    public void Neither_count_nor_duration_is_rejected()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            NetworkHelper.CreateEchoCampaign(new IcmpEchoCampaignOptions
            {
                Target = "127.0.0.1",
                RangeStartDate = DateOnly.FromDateTime(DateTime.Today),
                RangeEndDate = DateOnly.FromDateTime(DateTime.Today),
                Windows = [new EchoWindow(new TimeOnly(12, 0), 0)]
            }));
    }

    [Fact]
    public void Duration_only_is_accepted()
    {
        var campaign = NetworkHelper.CreateEchoCampaign(new IcmpEchoCampaignOptions
        {
            Target = "127.0.0.1",
            RangeStartDate = DateOnly.FromDateTime(DateTime.Today),
            RangeEndDate = DateOnly.FromDateTime(DateTime.Today),
            Windows = [new EchoWindow(new TimeOnly(12, 0), 0, TimeSpan.FromSeconds(1))]
        });
        Assert.Equal(TimeSpan.FromSeconds(1), campaign.Options.Windows[0].Duration);
        Assert.Equal(0, campaign.Options.Windows[0].Count);
    }

    [Fact]
    public void Old_recipe_without_durationMs_opens_count_only()
    {
        var path = Path.Combine(_root, "old.json");
        File.WriteAllText(path,
            """
            {"campaignId":"camp-old","target":"127.0.0.1","rangeStartDate":"2026-09-24","rangeEndDate":"2026-09-24","graceMinutes":15,"windows":[{"localTime":"12:00","count":2}]}
            """);
        var opened = NetworkHelper.OpenEchoCampaign(path);
        Assert.Null(opened.Options.Windows[0].Duration);
        Assert.Equal(2, opened.Options.Windows[0].Count);
    }
}
