using Vestigium.Helpers.Network;

namespace Vestigium.Helpers.Tests;

public sealed class NetworkBandwidthTests
{
    [Fact]
    public void Ten_MB_is_not_ten_Mb()
    {
        var bytes = NetworkHelper.Bandwidth(10, DataUnit.MB);
        var bits = NetworkHelper.Bandwidth(10, DataUnit.Mb);
        Assert.Equal(80_000_000m, bytes.Bits);
        Assert.Equal(10_000_000m, bits.Bits);
        var asMegabits = NetworkHelper.ConvertBandwidth(bytes, DataUnit.Mb);
        Assert.Equal(80_000_000m, asMegabits.Bits);
        Assert.Contains("80", asMegabits.Display, StringComparison.Ordinal);
    }

    [Fact]
    public void Twenty_Mbps_days30_uses_2592000_seconds()
    {
        var rate = NetworkHelper.Bandwidth(20, DataUnit.Mb);
        var month = NetworkHelper.VolumeFromRate(rate, BandwidthBasis.Days30);
        Assert.Equal(2_592_000, month.Seconds);
        Assert.Equal(20m * 1_000_000m * 2_592_000m, month.Volume.Bits);
        var day = NetworkHelper.VolumeFromRate(rate, BandwidthBasis.Day);
        var year = NetworkHelper.VolumeFromRate(rate, BandwidthBasis.Year365);
        Assert.Equal(86_400, day.Seconds);
        Assert.Equal(31_536_000, year.Seconds);
        Assert.Equal(month.Volume.Bits / 30m, day.Volume.Bits);
    }

    [Fact]
    public void Website_without_bots_is_zero_bot_hits()
    {
        var result = NetworkHelper.EstimateWebsite(new WebsiteTrafficQuery
        {
            PageSize = NetworkHelper.Bandwidth(1, DataUnit.MB),
            HumanHits = 1000
        });
        Assert.Equal(0, result.BotHits);
        Assert.Equal(1000, result.TotalHits);
        Assert.Equal(BandwidthBasis.Days30, result.Basis);
    }

    [Fact]
    public void Website_googlebot_hits_add_only_those_hits()
    {
        var result = NetworkHelper.EstimateWebsite(new WebsiteTrafficQuery
        {
            PageSize = NetworkHelper.Bandwidth(1, DataUnit.MB),
            HumanHits = 1000,
            BotRows = [new BotHitRow(CommonBotId.Googlebot, 10_000)]
        });
        Assert.Equal(10_000, result.BotHits);
        Assert.Equal(11_000, result.TotalHits);
        Assert.Contains(NetworkHelper.CommonBots, b => b.Id == CommonBotId.Googlebot);
    }

    [Fact]
    public void Zero_rate_and_nonzero_size_throws()
    {
        var size = NetworkHelper.Bandwidth(10, DataUnit.MB);
        var rate = NetworkHelper.Bandwidth(0, DataUnit.Mb);
        Assert.Throws<InvalidOperationException>(() => NetworkHelper.TransferTime(size, rate));
    }
}
