using Vestigium.Helpers.Analytics;
using Vestigium.Helpers.Network;

namespace Vestigium.Helpers.Tests;

public sealed class NetworkPr02BillTests
{
    [Fact]
    public void PR02_007_empty_samples_throw()
    {
        var empty = Assert.Throws<InvalidOperationException>(() =>
            NetworkHelper.BillP95([]));
        Assert.Contains("at least one sample", empty.Message, StringComparison.OrdinalIgnoreCase);

        Assert.Throws<InvalidOperationException>(() =>
            NetworkHelper.BillPercentile([], 0.95));

        Assert.Throws<ArgumentNullException>(() =>
            NetworkHelper.BillP95((IEnumerable<decimal>)null!));
    }

    [Fact]
    public void PR02_007_empty_series_throw()
    {
        Assert.Throws<ArgumentException>(() =>
            NumericSeries.FromDecimal([], "empty"));

        var series = NumericSeries.FromDecimal([10_000_000m], "one");
        var bill = NetworkHelper.BillP95(series);
        Assert.Equal(1, bill.SampleCount);
        Assert.NotEqual(0m, bill.Rate.Bits);
    }

    [Fact]
    public void BillPercentile_series_matches_sample_overload()
    {
        decimal[] samples = [10m, 20m, 30m, 40m];
        var series = NumericSeries.FromDecimal(samples, "bill");
        var fromSamples = NetworkHelper.BillPercentile(samples, 0.5);
        var fromSeries = NetworkHelper.BillPercentile(series, 0.5);
        Assert.Equal(fromSamples.Percentile, fromSeries.Percentile);
        Assert.Equal(fromSamples.Rate.Bits, fromSeries.Rate.Bits);
        Assert.Equal(fromSamples.SampleCount, fromSeries.SampleCount);
    }
}
