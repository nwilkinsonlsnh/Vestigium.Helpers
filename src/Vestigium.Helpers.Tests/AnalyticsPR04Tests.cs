using Vestigium.Helpers.Analytics;

namespace Vestigium.Helpers.Tests;

public sealed class AnalyticsPR04Tests
{
    [Fact]
    public void PR04_001_rejects_empty_p_and_gamma()
    {
        var q4 = NumericSeries.From(new[] { 5, 5, 5 }).Q4;
        var empty = Assert.Throws<InvalidOperationException>(() => q4.PercentileInterval(0.95));
        Assert.Equal(Quantiles.EmptySliceMessage, empty.Message);

        var series = NumericSeries.From(Enumerable.Range(1, 9));
        Assert.Throws<ArgumentOutOfRangeException>(() => series.PercentileInterval(-0.1));
        Assert.Throws<ArgumentOutOfRangeException>(() => series.PercentileInterval(1.1));
        Assert.Throws<ArgumentOutOfRangeException>(() => series.PercentileInterval(0.5, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => series.PercentileInterval(0.5, 1));
    }

    [Fact]
    public void PR04_002_order_stats_are_sample_values_and_meet_gamma_when_possible()
    {
        var series = NumericSeries.From(Enumerable.Range(1, 9));
        var mid = series.PercentileInterval(0.5);
        Assert.Equal(PercentileInterval.OrderStatisticMethod, mid.Method);
        Assert.True(mid.ReachedCoverage);
        Assert.InRange(mid.LowerRank, 1, 9);
        Assert.InRange(mid.UpperRank, mid.LowerRank, 9);
        Assert.Equal(series.Sorted[mid.LowerRank - 1], mid.Lower);
        Assert.Equal(series.Sorted[mid.UpperRank - 1], mid.Upper);
        Assert.True(mid.Coverage + 1e-12 >= mid.Level);
        Assert.Equal(
            PercentileInterval.CoverageOf(9, 0.5, mid.LowerRank, mid.UpperRank),
            mid.Coverage,
            12);
    }

    [Fact]
    public void PR04_002_p0_and_p1_are_the_ends()
    {
        var series = NumericSeries.From(Enumerable.Range(1, 9));
        var lo = series.PercentileInterval(0);
        Assert.Equal(1m, lo.Lower);
        Assert.Equal(1m, lo.Upper);
        Assert.True(lo.ReachedCoverage);

        var hi = series.PercentileInterval(1);
        Assert.Equal(9m, hi.Lower);
        Assert.Equal(9m, hi.Upper);
        Assert.True(hi.ReachedCoverage);
    }
}
