using System.Numerics;
using Vestigium.Helpers.Analytics;

namespace Vestigium.Helpers.Tests;

[Collection("Logger")]
public sealed class AnalyticsCoverageTests
{
    [Fact]
    public void NumberConvert_accepts_float_half_and_decimal()
    {
        var fromFloat = NumericSeries.From(new[] { 1.5f, 2.5f }, "f");
        Assert.Equal(2, fromFloat.Count);
        Assert.Equal(2.0, fromFloat.Full.Mean);
        var fromHalf = NumericSeries.From(new[] { (Half)1, (Half)3 }, "h");
        Assert.Equal(2d, fromHalf.Full.Mean);
        var fromDecimal = NumericSeries.From(new[] { 1.25m, 2.75m }, "d");
        Assert.Equal(2.0, fromDecimal.Full.Mean);
        Assert.Equal(1.25m, NumberConvert.ToDecimal(1.25m, 0));
    }

    [Fact]
    public void NumberConvert_rejects_non_finite_float_half_and_overflow()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => NumericSeries.From(new[] { 1f, float.NaN }));
        Assert.Throws<ArgumentOutOfRangeException>(() => NumericSeries.From(new[] { float.PositiveInfinity }));
        Assert.Throws<ArgumentOutOfRangeException>(() => NumericSeries.From(new[] { Half.NaN }));
        Assert.Throws<ArgumentOutOfRangeException>(() => NumericSeries.From(new[] { Half.PositiveInfinity }));
        var tooBig = BigInteger.Parse("79228162514264337593543950336");
        var overflow = Assert.Throws<ArgumentOutOfRangeException>(() => NumericSeries.From(new[] { tooBig }));
        Assert.Contains("decimal", overflow.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Quantiles_reject_empty_and_p_outside_unit_interval()
    {
        Assert.Throws<ArgumentException>(() => Quantiles.Inclusive(Array.Empty<decimal>(), 0.5));
        Assert.Throws<ArgumentOutOfRangeException>(() => Quantiles.Inclusive([1m, 2m], -0.01));
        Assert.Throws<ArgumentOutOfRangeException>(() => Quantiles.Inclusive([1m, 2m], 1.01));
        Assert.Equal(9m, Quantiles.Inclusive([9m], 0.37));
        Assert.Equal(1m, Quantiles.Inclusive([1m, 2m, 3m], 0));
        Assert.Equal(3m, Quantiles.Inclusive([1m, 2m, 3m], 1));
    }

    [Fact]
    public void NamedPercentiles_match_the_five_number_summary()
    {
        var series = NumericSeries.From(Enumerable.Range(1, 9), "odd");
        var named = series.Full.NamedPercentiles();
        Assert.Equal(series.Full.Q1, named[0.25]);
        Assert.Equal(series.Full.Median, named[0.50]);
        Assert.Equal(series.Full.Q3, named[0.75]);
        Assert.Equal(series.Full.Percentile(0.01), named[0.01]);
        Assert.Equal(series.Full.Percentile(0.99), named[0.99]);
    }

    [Fact]
    public void NamedPercentiles_on_an_empty_slice_throws()
    {
        var series = NumericSeries.From(new[] { 5, 5, 5 });
        Assert.True(series.Q2.IsEmpty);
        Assert.Equal(0, series.Q2.Count);
        Assert.Null(series.Q2.Mean);
        var ex = Assert.Throws<InvalidOperationException>(() => series.Q2.NamedPercentiles());
        Assert.Contains("empty slice", ex.Message, StringComparison.Ordinal);
        var emptyWilson = series.Q2.ProportionAbove(1m);
        Assert.False(emptyWilson.IsDefined);
        Assert.Equal("Wilson score", emptyWilson.Method);
        Assert.Null(emptyWilson.Width);
    }

    [Fact]
    public void PlanSampleSize_rejects_non_positive_margin_and_null_on_zero_s()
    {
        var series = NumericSeries.From(Enumerable.Range(1, 9), "odd");
        Assert.Throws<ArgumentOutOfRangeException>(() => series.SampleSizeForMeanMargin(0));
        Assert.Throws<ArgumentOutOfRangeException>(() => series.SampleSizeForMeanMargin(-1));
        var needed = series.SampleSizeForMeanMargin(0.5);
        Assert.NotNull(needed);
        Assert.True(needed >= 2);

        var flat = NumericSeries.From(new[] { 5, 5, 5 });
        Assert.Null(flat.SampleSizeForMeanMargin(0.1));
        var singleton = NumericSeries.From(new[] { 7 });
        Assert.Null(singleton.SampleSizeForMeanMargin(0.1));
        Assert.Null(singleton.MeanPValue(7));
        Assert.Null(singleton.MeanConfidenceLevelContaining(7));
    }

    [Fact]
    public void Finite_population_correction_narrows_the_mean_interval()
    {
        var series = NumericSeries.From(Enumerable.Range(1, 9), "odd");
        var infinite = series.Confidence(0.95);
        var fpc = series.Confidence(0.95, populationSize: 20);
        Assert.True(fpc.Mean.IsDefined);
        Assert.Contains("finite-population", fpc.Mean.Method, StringComparison.OrdinalIgnoreCase);
        Assert.True(fpc.Mean.Width < infinite.Mean.Width);
        Assert.Equal(5d, fpc.Mean.Estimate);

        Assert.Throws<ArgumentOutOfRangeException>(() => series.Confidence(0.95, populationSize: 0));
        Assert.Throws<ArgumentException>(() => series.Confidence(0.95, populationSize: 3));
    }

    [Fact]
    public void Singleton_confidence_defines_median_not_mean()
    {
        var series = NumericSeries.From(new[] { 7 });
        var ci = series.Confidence();
        Assert.False(ci.Mean.IsDefined);
        Assert.True(ci.Median.IsDefined);
        Assert.Equal(7d, ci.Median.Lower);
        Assert.Equal(7d, ci.Median.Upper);
        Assert.False(ci.Variance.IsDefined);
        Assert.False(ci.StdDev.IsDefined);
        Assert.Contains("n = 1", ci.Median.Method, StringComparison.Ordinal);
    }

    [Fact]
    public void ProportionAtLeast_counts_the_threshold()
    {
        var series = NumericSeries.From(new[] { 1, 2, 3, 4, 5 });
        var above = series.ProportionAbove(3m);
        var atLeast = series.ProportionAtLeast(3m);
        Assert.Equal(0.4, above.Estimate);
        Assert.Equal(0.6, atLeast.Estimate);
        Assert.True(atLeast.IsDefined);
        Assert.NotNull(atLeast.Width);
        Assert.True(atLeast.Width > 0);
    }

    [Fact]
    public void TimedValue_and_window_none_are_on_the_public_surface()
    {
        var t0 = new DateTimeOffset(2026, 9, 8, 12, 0, 0, TimeSpan.Zero);
        var series = NumericSeries.FromObservations(
        [
            new Observation(10m, t0),
            new Observation(20m, t0.AddSeconds(1))
        ], "timed");
        var points = series.TimeSeriesPoints();
        Assert.Equal(2, points.Count);
        Assert.Equal(t0, points[0].At);
        Assert.Equal(10m, points[0].Value);
        Assert.Equal(SeriesWindowKind.InferredFromTimestamps, series.Window.Kind);
        Assert.Equal(t0, series.Window.StartInclusive);
        Assert.Equal(SeriesWindowKind.None, SeriesWindow.None.Kind);
        Assert.Null(SeriesWindow.None.StartInclusive);
        var window = new SeriesWindow(t0, t0.AddSeconds(2), SeriesWindowKind.CallerSupplied);
        Assert.Equal(t0, window.StartInclusive);
        Assert.Equal(t0.AddSeconds(2), window.EndExclusive);
        Assert.Equal(SeriesWindowKind.CallerSupplied, window.Kind);

        var valuesOnly = NumericSeries.From(new[] { 1, 2, 3 });
        Assert.Equal(SeriesWindowKind.None, valuesOnly.Window.Kind);
        Assert.Empty(valuesOnly.TimeSeriesPoints());
    }

    [Fact]
    public void ConfidenceInterval_helpers_and_level_tostring()
    {
        var defined = ConfidenceInterval.Defined("Mean", 5, 4, 6, 0.95, "Student t");
        Assert.Equal(2d, defined.Width);
        Assert.True(defined.IsDefined);
        var flipped = ConfidenceInterval.Defined("Mean", 5, 6, 4, 0.95, "Student t");
        Assert.Equal(4d, flipped.Lower);
        Assert.Equal(6d, flipped.Upper);
        var undef = ConfidenceInterval.Undefined("Mean", 0.95, "Student t", estimate: 5);
        Assert.False(undef.IsDefined);
        Assert.Equal(5d, undef.Estimate);
        Assert.Null(undef.Width);
        Assert.Contains("%", ConfidenceLevel.Default.ToString());
        Assert.Equal(0.05, ConfidenceLevel.Default.Alpha, 10);
        double asDouble = ConfidenceLevel.Default;
        Assert.Equal(0.95, asDouble);
        ConfidenceLevel implicitLevel = 0.9;
        Assert.Equal(0.9, implicitLevel.Value);
    }

    [Fact]
    public void Empty_slice_control_limits_and_confidence_stay_defined_as_empty()
    {
        var series = NumericSeries.From(new[] { 5, 5, 5 });
        Assert.Throws<InvalidOperationException>(() => series.Q2.ControlLimits());
        var ci = series.Q2.Confidence();
        Assert.False(ci.Mean.IsDefined);
        Assert.False(ci.Median.IsDefined);
    }

    [Fact]
    public void FromDecimal_windowed_observations_and_point_helpers()
    {
        var fromDecimal = NumericSeries.FromDecimal([1.25m, 2.75m], "d");
        Assert.Equal(2, fromDecimal.Count);
        Assert.Equal(2.0, fromDecimal.Full.Mean);

        var t0 = new DateTimeOffset(2026, 9, 8, 12, 0, 0, TimeSpan.Zero);
        var obs = new Observation[]
        {
            new Observation(1m, t0.AddMinutes(-1)),
            new Observation(10m, t0),
            new Observation(20m, t0.AddSeconds(1)),
            new Observation(99m, t0.AddHours(1)),
            new Observation(5m, null)
        };
        var windowed = NumericSeries.FromObservations(obs, t0, t0.AddMinutes(1), "win");
        Assert.Equal(2, windowed.Count);
        Assert.Equal(SeriesWindowKind.CallerSupplied, windowed.Window.Kind);
        Assert.Throws<ArgumentException>(() =>
            NumericSeries.FromObservations(obs, t0.AddMinutes(1), t0, "bad"));
        Assert.Throws<ArgumentException>(() =>
            NumericSeries.FromObservations(Array.Empty<Observation>(), "empty"));

        var series = NumericSeries.From(new[] { 3, 1, 2 }, "pts");
        Assert.Equal(new[] { 3d, 1d, 2d }, series.SampleOrderPoints().Select(p => p.Y).ToArray());
        Assert.Equal(new[] { 1d, 2d, 3d }, series.SortedPoints().Select(p => p.Y).ToArray());
        Assert.NotEmpty(series.HistogramRelativePoints());
        Assert.All(series.HistogramRelativePoints(), p => Assert.InRange(p.Y, 0, 1));

        var limits = series.ControlLimits(floor: -100);
        Assert.Equal(-100, limits.Floor);
        Assert.True(limits.Lower > -100);
        Assert.Throws<ArgumentOutOfRangeException>(() => series.ControlLimits(k: 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => series.ControlLimits(k: -1));
        Assert.Throws<ArgumentException>(() => series.ControlLimits(ControlLimitMethod.CallerSupplied));
        var floorClamp = NumericSeries.From(new[] { 1, 2, 3 }).ControlLimits(k: 3, floor: 0);
        Assert.Equal(0, floorClamp.Lower);

        var flat = NumericSeries.From(new[] { 7, 7, 7, 7 }, "flat");
        Assert.NotEmpty(flat.Full.Frequency.Histogram);
        Assert.Equal(7m, flat.Full.Frequency.Mode);
        var distinct = NumericSeries.From(new[] { 1, 2, 3, 4 }, "distinct");
        Assert.Null(distinct.Full.Frequency.Mode);
        var unique = NumericSeries.From(new[] { 1, 1, 2, 3 }, "mode");
        Assert.Equal(1m, unique.Full.Frequency.Mode);
        Assert.True(unique.Full.Frequency.HasUniqueMode);
        Assert.Equal(0, FrequencyTable.Empty.DistinctCount);
    }

}
