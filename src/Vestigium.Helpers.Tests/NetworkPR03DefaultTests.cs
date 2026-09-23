using Vestigium.Helpers.Network;

namespace Vestigium.Helpers.Tests;

public sealed class NetworkPR03DefaultTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "pr03-default-" + Guid.NewGuid().ToString("N"));

    public NetworkPR03DefaultTests()
    {
        Directory.CreateDirectory(_root);
        NetworkTestHooks.CampaignRoot = _root;
        NetworkTestHooks.ShareRoot = _root;
    }

    public void Dispose()
    {
        NetworkTestHooks.Reset();
        try { Directory.Delete(_root, true); } catch { }
    }

    [Fact]
    public void PR03_003_default_scales_1_gib_from_identical_probes()
    {
        const double bytesPerSecond = 100_000_000d;
        NetworkTestHooks.ProbeBytesPerSecond = [bytesPerSecond, bytesPerSecond, bytesPerSecond, bytesPerSecond];

        var share = Path.Combine(_root, "share");
        Directory.CreateDirectory(share);
        var planned = NetworkHelper.Bandwidth(1, DataUnit.GiB);
        var campaign = NetworkHelper.CreateShareCampaign(new ShareCampaignOptions
        {
            Target = new FileShareTarget { Directory = share },
            PlannedSize = planned,
            Mode = ShareCampaignMode.Default,
            Efficiency = 1.0,
            ResultsPath = Path.Combine(_root, "default.jsonl")
        });

        var result = campaign.RunAsync().GetAwaiter().GetResult();
        var expectedRate = NetworkHelper.Bandwidth((decimal)(bytesPerSecond * 8d), DataUnit.Bit);
        var expected = NetworkHelper.TransferTime(planned, expectedRate);

        Assert.Equal(NetworkJobStatus.Success, result.Status);
        Assert.Equal(expected.Duration, result.MeasuredDuration);
        Assert.NotNull(result.MeasuredRate);
        Assert.Equal(expectedRate.Bits, result.MeasuredRate!.Bits);
        Assert.Contains("64 MiB", result.Disclaimer, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Advanced", result.Disclaimer, StringComparison.OrdinalIgnoreCase);
        Assert.True(File.Exists(result.ResultsPath));
    }

    [Fact]
    public void PR03_003_efficiency_lengthens_duration()
    {
        const double bytesPerSecond = 100_000_000d;
        NetworkTestHooks.ProbeBytesPerSecond = [bytesPerSecond, bytesPerSecond, bytesPerSecond, bytesPerSecond];
        var share = Path.Combine(_root, "share2");
        Directory.CreateDirectory(share);
        var planned = NetworkHelper.Bandwidth(1, DataUnit.GiB);
        var full = NetworkHelper.CreateShareCampaign(new ShareCampaignOptions
        {
            Target = new FileShareTarget { Directory = share },
            PlannedSize = planned,
            Efficiency = 1.0,
            ResultsPath = Path.Combine(_root, "full.jsonl")
        }).RunAsync().GetAwaiter().GetResult();

        var half = NetworkHelper.CreateShareCampaign(new ShareCampaignOptions
        {
            Target = new FileShareTarget { Directory = share },
            PlannedSize = planned,
            Efficiency = 0.5,
            ResultsPath = Path.Combine(_root, "half.jsonl")
        }).RunAsync().GetAwaiter().GetResult();

        Assert.Equal(full.MeasuredDuration.Ticks * 2, half.MeasuredDuration.Ticks);
    }
}
