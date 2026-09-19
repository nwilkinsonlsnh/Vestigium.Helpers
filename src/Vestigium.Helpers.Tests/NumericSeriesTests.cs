using Vestigium.Helpers.Analytics;

namespace Vestigium.Helpers.Tests;

public sealed class NumericSeriesTests
{
    [Fact]
    public void Identity_is_stable()
        => Assert.Equal("Vestigium.Helpers.Analytics", AnalyticsHelper.Identity);

    [Fact]
    public void Five_number_summary_matches_percentile_inc_odd_n()
    {
        var series = NumericSeries.From(new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9 }, "odd");

        Assert.Equal("odd", series.Name);
        Assert.Equal(9, series.Count);
        Assert.Equal(1m, series.Full.Min);
        Assert.Equal(3m, series.Full.Q1);
        Assert.Equal(5m, series.Full.Median);
        Assert.Equal(7m, series.Full.Q3);
        Assert.Equal(9m, series.Full.Max);
        Assert.Equal(8m, series.Full.Range);
        Assert.Equal(4m, series.Full.Iqr);
        Assert.Equal(5d, series.Full.Mean);
        Assert.Equal(7.5, series.Full.Variance);
        Assert.Equal(0d, series.Full.Skewness);
        Assert.True(series.Full.ExcessKurtosis < 0);
    }

    [Fact]
    public void Five_number_summary_matches_percentile_inc_even_n()
    {
        var series = AnalyticsHelper.From(new[] { 1, 2, 3, 4 });

        Assert.Equal(1.75m, series.Full.Q1);
        Assert.Equal(2.5m, series.Full.Median);
        Assert.Equal(3.25m, series.Full.Q3);
        Assert.Equal(1.5m, series.Full.Iqr);
    }

    [Fact]
    public void Percentile_interpolation_stays_in_decimal()
    {
        var series = NumericSeries.From(Enumerable.Range(1, 1000));

        Assert.Equal(1m, series.Full.Percentile(0));
        Assert.Equal(1000m, series.Full.Percentile(1));
        Assert.Equal(50.95m, series.Full.Percentile(0.05));
        Assert.Equal(900.10m, series.Full.Percentile(0.90));
        var text = series.Full.Percentile(0.05).ToString(System.Globalization.CultureInfo.InvariantCulture);
        Assert.DoesNotContain("000000", text);
    }

    [Fact]
    public void Quartile_bands_split_on_full_series_fences()
    {
        var series = NumericSeries.From(new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9 });

        Assert.Equal(new[] { 1m, 2m, 3m }, series.Q1.Values);
        Assert.Equal(new[] { 4m, 5m }, series.Q2.Values);
        Assert.Equal(new[] { 6m, 7m }, series.Q3.Values);
        Assert.Equal(new[] { 8m, 9m }, series.Q4.Values);
        Assert.Equal(new[] { 3m, 4m, 5m, 6m, 7m }, series.Iqr.Values);

        Assert.Equal(2m, series.Q1.Range);
        Assert.NotNull(series.Q1.Frequency);
        Assert.Equal(3, series.Q1.Frequency.DistinctCount);
    }

    [Fact]
    public void Degenerate_series_collapses_into_q1_and_iqr()
    {
        var series = NumericSeries.From(new[] { 5, 5, 5 });

        Assert.Equal(0m, series.Full.Range);
        Assert.Equal(0m, series.Full.Iqr);
        Assert.Null(series.Full.Skewness);
        Assert.Null(series.Full.ExcessKurtosis);
        Assert.Equal(3, series.Q1.Count);
        Assert.Equal(0, series.Q2.Count);
        Assert.Equal(0, series.Q3.Count);
        Assert.Equal(0, series.Q4.Count);
        Assert.Equal(3, series.Iqr.Count);
        Assert.True(series.Full.Frequency.HasUniqueMode);
        Assert.Equal(5m, series.Full.Frequency.Mode);
    }

    [Fact]
    public void Mean_confidence_interval_uses_student_t()
    {
        var series = NumericSeries.From(Enumerable.Range(1, 9));
        var ci = series.Confidence(0.95);

        Assert.Equal(0.95, ci.Level.Value);
        Assert.True(ci.Mean.IsDefined);
        Assert.Equal(5d, ci.Mean.Estimate);
        Assert.Equal(2.8949, ci.Mean.Lower!.Value, 4);
        Assert.Equal(7.1051, ci.Mean.Upper!.Value, 4);
        Assert.True(ci.Median.IsDefined);
        Assert.True(ci.StdDev.IsDefined);
        Assert.True(ci.Variance.IsDefined);
    }

    [Fact]
    public void Confidence_level_is_rejected_at_the_boundaries()
    {
        var series = NumericSeries.From(new[] { 1d, 2d, 3d });
        Assert.Throws<ArgumentOutOfRangeException>(() => series.Confidence(0));
        Assert.Throws<ArgumentOutOfRangeException>(() => series.Confidence(1));
        Assert.Throws<ArgumentOutOfRangeException>(() => ConfidenceLevel.Of(-0.1));
    }

    [Fact]
    public void Mean_confidence_level_containing_is_the_t_test_dual()
    {
        var series = NumericSeries.From(Enumerable.Range(1, 9));
        var p = series.MeanPValue(5d);
        var gamma = series.MeanConfidenceLevelContaining(5d);

        Assert.NotNull(p);
        Assert.InRange(p!.Value, 0.999, 1.000);
        Assert.NotNull(gamma);
        Assert.InRange(gamma!.Value, 0.0, 0.001);
    }

    [Fact]
    public void Rejects_empty_and_non_finite()
    {
        Assert.Throws<ArgumentException>(() => NumericSeries.From(Array.Empty<int>()));
        Assert.Throws<ArgumentNullException>(() => NumericSeries.From<int>(null!));
        Assert.Throws<ArgumentOutOfRangeException>(() => NumericSeries.From(new[] { 1d, double.NaN }));
        Assert.Throws<ArgumentOutOfRangeException>(() => NumericSeries.From(new[] { double.PositiveInfinity }));
    }

    [Fact]
    public void Outlier_is_flagged_by_tukey_fences()
    {
        var series = NumericSeries.From(new[] { 1, 2, 3, 4, 5, 100 });
        Assert.Contains(100m, series.Full.Outliers);
        Assert.Contains(100m, series.Full.HighOutliers);
        Assert.Empty(series.Full.LowOutliers);
        Assert.DoesNotContain(1m, series.Full.Outliers);
        Assert.Equal(new[] { 5 }, series.Full.HighOutlierIndexes);
        Assert.Empty(series.Full.LowOutlierIndexes);
        Assert.Equal(new[] { 5 }, series.Full.OutlierIndexes);
    }

    [Fact]
    public void Right_skew_outlier_index_is_the_last_encounter()
    {
        var series = NumericSeries.From(new[] { 10, 11, 11, 12, 12, 12, 13, 13, 14, 40 });
        Assert.Equal(new[] { 9 }, series.Full.HighOutlierIndexes);
        Assert.Empty(series.Q4.HighOutlierIndexes);
    }

    [Fact]
    public void Wilson_proportion_is_defined()
    {
        var series = NumericSeries.From(new[] { 1, 2, 3, 4, 5 });
        var interval = series.ProportionAbove(3m, 0.95);
        Assert.True(interval.IsDefined);
        Assert.Equal(0.4, interval.Estimate);
        Assert.True(interval.Lower < interval.Estimate);
        Assert.True(interval.Upper > interval.Estimate);
    }

    [Fact]
    public void Proportion_above_seven_on_one_to_nine_is_two_ninths()
    {
        var series = NumericSeries.From(Enumerable.Range(1, 9));
        var interval = series.ProportionAbove(7m);
        Assert.True(interval.IsDefined);
        Assert.Equal(2d / 9d, interval.Estimate);
    }

    [Fact]
    public void Census_collapses_the_mean_interval()
    {
        var series = NumericSeries.From(Enumerable.Range(1, 9));
        var ci = series.Confidence(0.95, populationSize: 9);
        Assert.True(ci.Mean.IsDefined);
        Assert.Equal(5d, ci.Mean.Lower);
        Assert.Equal(5d, ci.Mean.Upper);
    }

    [Fact]
    public void Named_percentiles_match_percentile()
    {
        var series = NumericSeries.From(Enumerable.Range(1, 9));
        var named = series.Full.NamedPercentiles();
        foreach (var p in new[] { 0.01, 0.05, 0.10, 0.25, 0.50, 0.75, 0.90, 0.95, 0.99 })
        {
            Assert.True(named.ContainsKey(p));
            Assert.Equal(series.Full.Percentile(p), named[p]);
        }
    }

    [Fact]
    public void Empty_band_confidence_is_undefined_and_percentiles_throw()
    {
        var series = NumericSeries.From(new[] { 5, 5, 5 });
        var ci = series.Q4.Confidence();
        Assert.False(ci.Mean.IsDefined);
        Assert.False(ci.Median.IsDefined);
        Assert.False(ci.Variance.IsDefined);
        Assert.False(ci.StdDev.IsDefined);
        Assert.Empty(series.Q4.OutlierIndexes);
        Assert.Throws<InvalidOperationException>(() => series.Q4.Percentile(0.95));
        Assert.Throws<InvalidOperationException>(() => series.Q4.NamedPercentiles());
    }

    [Fact]
    public void Sample_size_planner_uses_current_s()
    {
        var series = NumericSeries.From(Enumerable.Range(1, 9));
        var n = series.SampleSizeForMeanMargin(0.5);
        Assert.NotNull(n);
        Assert.True(n >= 2);
        Assert.Throws<ArgumentOutOfRangeException>(() => series.SampleSizeForMeanMargin(0));
        Assert.Throws<ArgumentOutOfRangeException>(() => series.SampleSizeForMeanMargin(-1));
    }

    [Fact]
    public void Ecdf_ends_at_one()
    {
        var series = NumericSeries.From(new[] { 1, 2, 3, 4 });
        var ecdf = series.EcdfPoints();
        Assert.Equal(4, ecdf.Count);
        Assert.Equal(1.0, ecdf[^1].Y);
        Assert.Equal(1.0, ecdf[0].X);
        Assert.Equal(4.0, ecdf[^1].X);
    }

    [Fact]
    public void Histogram_trend_has_one_point_per_bin()
    {
        var series = NumericSeries.From(Enumerable.Range(1, 9));
        var hist = series.HistogramPoints();
        var trend = series.HistogramTrendPoints();
        Assert.Equal(hist.Count, trend.Count);
        Assert.Equal(hist[0].X, trend[0].X);
        Assert.Equal(hist[^1].X, trend[^1].X);
    }

    [Fact]
    public void Pareto_cumulative_ends_at_one()
    {
        var series = NumericSeries.From(new[] { 10, 11, 11, 12, 12, 12, 13, 13, 14, 40 });
        var pareto = series.ParetoPoints();
        Assert.NotEmpty(pareto);
        Assert.Equal(1, pareto[0].Rank);
        Assert.Equal(1.0, pareto[^1].CumulativeShare, 12);
        for (var i = 1; i < pareto.Count; i++)
            Assert.True(pareto[i - 1].Count >= pareto[i].Count);
    }

    [Fact]
    public void Values_only_series_has_no_timestamps()
    {
        var series = NumericSeries.From(new[] { 1.0, 2.0, 3.0 });
        Assert.False(series.HasTimestamps);
        Assert.Empty(series.TimeSeriesPoints());
        Assert.Throws<InvalidOperationException>(() =>
            series.Slice(DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddSeconds(1)));
    }

    [Fact]
    public void Slice_keeps_the_middle_observation()
    {
        var t0 = new DateTimeOffset(2026, 9, 7, 15, 0, 0, TimeSpan.Zero);
        var series = NumericSeries.FromObservations(
        [
            new Observation(10m, t0),
            new Observation(20m, t0.AddSeconds(1)),
            new Observation(30m, t0.AddSeconds(2)),
            new Observation(99m, At: null)
        ]);

        var sliced = series.Slice(t0.AddMilliseconds(500), t0.AddMilliseconds(1500));
        Assert.Equal(new[] { 20m }, sliced.Values);
        Assert.Equal(SeriesWindowKind.CallerSupplied, sliced.Window.Kind);
        Assert.Equal(3, series.TimeSeriesPoints().Count);
    }

    [Fact]
    public void Time_series_points_follow_the_clock()
    {
        var t0 = new DateTimeOffset(2026, 9, 19, 14, 0, 0, TimeSpan.Zero);
        var series = NumericSeries.FromObservations(
        [
            new Observation(30m, t0.AddSeconds(2)),
            new Observation(10m, t0),
            new Observation(20m, t0.AddSeconds(1)),
            new Observation(99m, At: null),
            new Observation(11m, t0)
        ]);

        var points = series.TimeSeriesPoints();
        Assert.Equal(4, points.Count);
        Assert.Equal(new[] { 10m, 11m, 20m, 30m }, points.Select(p => p.Value).ToArray());
        Assert.Equal(t0, points[0].At);
        Assert.Equal(t0, points[1].At);
        Assert.Equal(t0.AddSeconds(1), points[2].At);
        Assert.Equal(t0.AddSeconds(2), points[3].At);
    }

    [Fact]
    public void Right_skew_sample_flags_the_slow_tail()
    {
        var series = NumericSeries.From(new[] { 10, 11, 11, 12, 12, 12, 13, 13, 14, 40 });
        Assert.True(series.Full.Skewness > 0);
        Assert.Equal(40m, series.Full.Max);
        Assert.Contains(40m, series.Full.HighOutliers);
        Assert.True(series.Full.Percentile(0.95) > series.Full.Median);
    }

    [Fact]
    public void Snapshot_lists_are_frozen()
    {
        var series = NumericSeries.From(new[] { 1, 2, 3 });

        Assert.True(((IList<decimal>)series.Values).IsReadOnly);
        Assert.True(((IList<decimal>)series.Sorted).IsReadOnly);
        Assert.True(((IList<decimal>)series.Full.Values).IsReadOnly);
        Assert.True(((IList<decimal>)series.Full.Sorted).IsReadOnly);
        Assert.True(((IList<decimal>)series.Full.HighOutliers).IsReadOnly);
        Assert.Throws<NotSupportedException>(() => ((IList<decimal>)series.Values)[0] = 99m);
        Assert.Throws<NotSupportedException>(() => ((IList<decimal>)series.Full.Values)[0] = 99m);

        var t0 = new DateTimeOffset(2026, 9, 19, 14, 0, 0, TimeSpan.Zero);
        var timed = NumericSeries.FromObservations([new Observation(1m, t0)]);
        Assert.True(((IList<DateTimeOffset?>)timed.Times).IsReadOnly);
        Assert.Throws<NotSupportedException>(() => ((IList<DateTimeOffset?>)timed.Times)[0] = null);
    }
}
