using Vestigium.Helpers.Analytics;

namespace Vestigium.Helpers.Tests;

public sealed class AnalyticsPR02Tests
{
    [Fact]
    public void PR02_001_computed_limit_lists_are_frozen()
    {
        var series = NumericSeries.From([1, 2, 3, 4, 5, 6, 7, 8, 9]);
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

    [Fact]
    public void PR02_002_log_indexes_join_when_at_most_32()
    {
        Assert.Equal("0,1,2", ControlLimits.FormatOutOfControlIndexes([.. Enumerable.Range(0, 3)]));
        Assert.Equal(string.Join(",", Enumerable.Range(0, 32)), ControlLimits.FormatOutOfControlIndexes([
            .. Enumerable.Range(0, 32)
        ]));
    }

    [Fact]
    public void PR02_002_log_indexes_truncate_past_32()
    {
        Assert.Equal("n=33 (truncated)", ControlLimits.FormatOutOfControlIndexes([.. Enumerable.Range(0, 33)]));
        Assert.Equal("", ControlLimits.FormatOutOfControlIndexes([]));
    }

    [Fact]
    public void PR02_003_empty_percentile_is_one_invalid_operation()
    {
        var q4 = NumericSeries.From([5, 5, 5]).Q4;
        var percentile = Assert.Throws<InvalidOperationException>(() => q4.Percentile(0.95));
        var named = Assert.Throws<InvalidOperationException>(() => q4.NamedPercentiles());
        var rank = Assert.Throws<InvalidOperationException>(() => q4.PercentileRank(5m));

        Assert.Equal(Quantiles.EmptySliceMessage, percentile.Message);
        Assert.Equal(Quantiles.EmptySliceMessage, named.Message);
        Assert.Equal(Quantiles.EmptySliceMessage, rank.Message);
    }

    [Fact]
    public void PR02_004_descriptor_overflow_is_argument_out_of_range()
    {
        var ex = Assert.Throws<ArgumentOutOfRangeException>(() =>
            NumericSeries.From([decimal.MaxValue, decimal.MaxValue]));
        Assert.Contains("overflowed", ex.Message, StringComparison.OrdinalIgnoreCase);
        Assert.IsType<OverflowException>(ex.InnerException);
    }

    [Fact]
    public void PR02_006_sanitize_name_strips_controls_and_truncates()
    {
        Assert.Null(AnalyticsLog.SanitizeName(null));
        Assert.Null(AnalyticsLog.SanitizeName("   "));
        Assert.Null(AnalyticsLog.SanitizeName("\r\n\t"));
        Assert.Equal("rtt ms", AnalyticsLog.SanitizeName("rtt\r\nms"));
        Assert.Equal("ok", AnalyticsLog.SanitizeName("  ok  "));
        Assert.Equal(AnalyticsLog.MaxNameLength, AnalyticsLog.SanitizeName(new string('a', 200))!.Length);
    }

    [Fact]
    public void PR02_006_name_log_property_is_sanitized()
    {
        var props = AnalyticsLog.Props(("name", "rtt\nms"), ("n", "3"));
        Assert.Equal("rtt ms", props["name"]);
        Assert.Equal("3", props["n"]);
    }
}
