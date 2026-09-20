using Vestigium.Helpers.Analytics;
using Vestigium.Helpers.Network;

namespace Vestigium.Helpers.Tests;

public sealed class NetworkPR02BillTests
{
    [Fact]
    public void PR02_007_empty_samples_throw()
    {
        var empty = Assert.Throws<InvalidOperationException>(() =>
            NetworkHelper.BillP95(Array.Empty<decimal>()));
        Assert.Contains("at least one sample", empty.Message, StringComparison.OrdinalIgnoreCase);

        Assert.Throws<InvalidOperationException>(() =>
            NetworkHelper.BillPercentile(Array.Empty<decimal>(), 0.95));

        Assert.Throws<ArgumentNullException>(() =>
            NetworkHelper.BillP95((IEnumerable<decimal>)null!));
    }

    [Fact]
    public void PR02_007_empty_series_throw()
    {
        var built = Assert.Throws<ArgumentException>(() =>
            NumericSeries.FromDecimal(Array.Empty<decimal>(), "empty"));
        Assert.False(string.IsNullOrWhiteSpace(built.Message));

        var series = NumericSeries.FromDecimal([10_000_000m], "one");
        var bill = NetworkHelper.BillP95(series);
        Assert.Equal(1, bill.SampleCount);
        Assert.NotEqual(0m, bill.Rate.BitsPerSecond);
    }
}
