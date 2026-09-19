using Vestigium.Helpers.Analytics;

namespace Vestigium.Helpers.Tests;

public sealed class AnalyticsS5Tests
{
    [Fact]
    public void Against_scores_caller_fences_without_changing_them()
    {
        var fences = ControlLimits.FromCaller(12, 30, 0);
        var scored = fences.Against([12m, 12m, 40m]);

        Assert.Equal(new[] { 2 }, scored.OutOfControlIndexes);
        Assert.Equal(1, scored.OutOfControlCount);
        Assert.Equal(12, scored.Center);
        Assert.Equal(30, scored.Upper);
        Assert.Equal(0, scored.Lower);
        Assert.Equal(ControlLimitMethod.CallerSupplied, scored.Method);
        Assert.Equal(0, fences.OutOfControlCount);
        Assert.Empty(fences.OutOfControlIndexes);
    }

    [Fact]
    public void Against_empty_or_null_is_zero()
    {
        var fences = ControlLimits.FromCaller(12, 30, 0);
        var empty = fences.Against([]);
        Assert.Equal(0, empty.OutOfControlCount);
        Assert.Empty(empty.OutOfControlIndexes);

        var missing = fences.Against(null);
        Assert.Equal(0, missing.OutOfControlCount);
        Assert.Empty(missing.OutOfControlIndexes);
    }

    [Fact]
    public void Against_can_score_computed_limits_too()
    {
        var series = NumericSeries.From(Enumerable.Range(1, 9));
        var limits = series.ControlLimits(ControlLimitMethod.MovingRange);
        var again = limits.Against(series.Values);
        Assert.Equal(limits.OutOfControlIndexes, again.OutOfControlIndexes);
        Assert.Equal(limits.Center, again.Center);
        Assert.Equal(limits.Upper, again.Upper);
        Assert.Equal(limits.Lower, again.Lower);
    }

    [Fact]
    public void Percentile_rank_on_one_to_nine()
    {
        var series = NumericSeries.From(Enumerable.Range(1, 9));
        Assert.Equal(5d / 9d, series.Full.PercentileRank(5m));
        Assert.Equal(1d, series.Full.PercentileRank(9m));
        Assert.Equal(0d, series.Full.PercentileRank(0m));
        Assert.Equal(1d, series.Full.PercentileRank(100m));
    }

    [Fact]
    public void Percentile_rank_empty_q4_throws()
    {
        var series = NumericSeries.From(new[] { 5, 5, 5 });
        Assert.Throws<InvalidOperationException>(() => series.Q4.PercentileRank(5m));
    }
}
