using Vestigium.Helpers.Analytics;

namespace Vestigium.Helpers.Tests;

public sealed class AnalyticsPR04Tests
{
    [Fact]
    public void PR04_001_sample_range_is_the_stand_in()
    {
        var series = NumericSeries.From(Enumerable.Range(1, 9));
        var interval = series.PercentileInterval(0.95);

        Assert.Equal(0.95, interval.P);
        Assert.Equal(ConfidenceLevel.DefaultValue, interval.Level);
        Assert.Equal(1m, interval.Lower);
        Assert.Equal(9m, interval.Upper);
        Assert.Equal(1, interval.LowerRank);
        Assert.Equal(9, interval.UpperRank);
        Assert.Equal(PercentileInterval.SampleRangeMethod, interval.Method);
        Assert.False(interval.ReachedCoverage);
    }

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
}
