using Vestigium.Helpers.Analytics;

namespace Vestigium.Helpers.Tests;

public sealed class AnalyticsPR02Tests
{
    [Fact]
    public void PR02_001_computed_limit_lists_are_frozen()
    {
        var series = NumericSeries.From(new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9 });
        var mr = series.ControlLimits(ControlLimitMethod.MovingRange);

        Assert.True(((IList<int>)mr.OutOfControlIndexes).IsReadOnly);
        Assert.True(((IList<double>)mr.MovingRanges).IsReadOnly);
        Assert.Throws<NotSupportedException>(() => ((IList<int>)mr.OutOfControlIndexes)[0] = 99);
        Assert.Throws<NotSupportedException>(() => ((IList<double>)mr.MovingRanges)[0] = 99d);
    }

    [Fact]
    public void PR02_001_against_lists_are_frozen()
    {
        var scored = ControlLimits.FromCaller(12, 30, 0).Against([12m, 40m]);
        Assert.True(((IList<int>)scored.OutOfControlIndexes).IsReadOnly);
        Assert.True(((IList<double>)scored.MovingRanges).IsReadOnly);
        Assert.Throws<NotSupportedException>(() => ((IList<int>)scored.OutOfControlIndexes)[0] = 99);
    }
}
