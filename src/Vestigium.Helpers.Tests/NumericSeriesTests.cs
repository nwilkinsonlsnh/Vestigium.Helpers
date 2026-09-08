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
    public void Census_collapses_the_mean_interval()
    {
        var series = NumericSeries.From(Enumerable.Range(1, 9));
        var ci = series.Confidence(0.95, populationSize: 9);
        Assert.True(ci.Mean.IsDefined);
        Assert.Equal(5d, ci.Mean.Lower);
        Assert.Equal(5d, ci.Mean.Upper);
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
    public void Right_skew_sample_flags_the_slow_tail()
    {
        var series = NumericSeries.From(new[] { 10, 11, 11, 12, 12, 12, 13, 13, 14, 40 });
        Assert.True(series.Full.Skewness > 0);
        Assert.Equal(40m, series.Full.Max);
        Assert.Contains(40m, series.Full.HighOutliers);
        Assert.True(series.Full.Percentile(0.95) > series.Full.Median);
    }
}
