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

    [Theory]
    [InlineData(DataUnit.Bit)]
    [InlineData(DataUnit.Byte)]
    [InlineData(DataUnit.Kb)]
    [InlineData(DataUnit.KB)]
    [InlineData(DataUnit.Kib)]
    [InlineData(DataUnit.KiB)]
    [InlineData(DataUnit.Mb)]
    [InlineData(DataUnit.MB)]
    [InlineData(DataUnit.Mib)]
    [InlineData(DataUnit.MiB)]
    [InlineData(DataUnit.Gb)]
    [InlineData(DataUnit.GB)]
    [InlineData(DataUnit.Gib)]
    [InlineData(DataUnit.GiB)]
    [InlineData(DataUnit.Tb)]
    [InlineData(DataUnit.TB)]
    [InlineData(DataUnit.Tib)]
    [InlineData(DataUnit.TiB)]
    public void Every_unit_round_trips(DataUnit unit)
    {
        var amount = NetworkHelper.Bandwidth(1, unit);
        Assert.True(amount.Bits > 0);
        var again = NetworkHelper.ConvertBandwidth(amount, unit);
        Assert.Equal(amount.Bits, again.Bits);
        Assert.False(string.IsNullOrWhiteSpace(again.Display));
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
        var back = NetworkHelper.RateFromVolume(month.Volume, BandwidthBasis.Days30);
        Assert.Equal(month.Volume.Bits, back.Volume.Bits);
        Assert.Equal(2_592_000, NetworkHelper.BandwidthSeconds(BandwidthBasis.Days30));
    }

    [Fact]
    public void Transfer_time_and_required_rate()
    {
        var size = NetworkHelper.Bandwidth(10, DataUnit.MB);
        var rate = NetworkHelper.Bandwidth(10, DataUnit.Mb);
        var time = NetworkHelper.TransferTime(size, rate);
        Assert.Equal(TimeSpan.FromSeconds(8), time.Duration);
        var need = NetworkHelper.RequiredRate(size, TimeSpan.FromSeconds(8));
        Assert.Equal(size.Bits / 8m, need.Bits);
        var moved = NetworkHelper.Transferred(rate, TimeSpan.FromSeconds(8));
        Assert.Equal(size.Bits, moved.Bits);
        var zero = NetworkHelper.TransferTime(NetworkHelper.Bandwidth(0, DataUnit.MB), rate);
        Assert.Equal(TimeSpan.Zero, zero.Duration);
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
            BotRows = [new BotHitRow(CommonBotId.Googlebot, 10_000)],
            Basis = BandwidthBasis.Year365,
            PeakFactor = 2
        });
        Assert.Equal(10_000, result.BotHits);
        Assert.Equal(11_000, result.TotalHits);
        Assert.Equal(BandwidthBasis.Year365, result.Basis);
        Assert.Contains(NetworkHelper.CommonBots, b => b.Id == CommonBotId.Googlebot);
    }

    [Fact]
    public void Website_rejects_negatives()
    {
        Assert.Throws<ArgumentNullException>(() => NetworkHelper.EstimateWebsite(null!));
        Assert.Throws<ArgumentOutOfRangeException>(() => NetworkHelper.EstimateWebsite(new WebsiteTrafficQuery { HumanHits = -1 }));
        Assert.Throws<ArgumentOutOfRangeException>(() => NetworkHelper.EstimateWebsite(new WebsiteTrafficQuery
        {
            BotRows = [new BotHitRow(CommonBotId.Other, -1)]
        }));
        Assert.Throws<ArgumentOutOfRangeException>(() => NetworkHelper.Bandwidth(-1, DataUnit.Mb));
        Assert.Throws<InvalidOperationException>(() =>
            NetworkHelper.RequiredRate(NetworkHelper.Bandwidth(1, DataUnit.MB), TimeSpan.Zero));
    }

    [Fact]
    public void Zero_rate_and_nonzero_size_throws()
    {
        var size = NetworkHelper.Bandwidth(10, DataUnit.MB);
        var rate = NetworkHelper.Bandwidth(0, DataUnit.Mb);
        Assert.Throws<InvalidOperationException>(() => NetworkHelper.TransferTime(size, rate));
    }
}
