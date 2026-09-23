using Vestigium.Helpers.Analytics;

namespace Vestigium.Helpers.Tests;

public sealed class AnalyticsPR04Tests
{
    [Fact]
    public void PR04_001_rejects_empty_p_and_gamma()
    {
        var q4 = NumericSeries.From([5, 5, 5]).Q4;
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

    [Fact]
    public void PR04_003_n9_p50_is_x2_to_x7()
    {
        var series = NumericSeries.From(Enumerable.Range(1, 9));
        var mid = series.PercentileInterval(0.5, 0.95);
        Assert.Equal(2, mid.LowerRank);
        Assert.Equal(7, mid.UpperRank);
        Assert.Equal(2m, mid.Lower);
        Assert.Equal(7m, mid.Upper);
        Assert.Equal(0.9609375, mid.Coverage, 12);
        Assert.True(mid.ReachedCoverage);
        Assert.Contains(mid.Lower, series.Sorted);
        Assert.Contains(mid.Upper, series.Sorted);
    }

    [Fact]
    public void PR04_003_n9_p95_is_x7_to_x9()
    {
        var series = NumericSeries.From(Enumerable.Range(1, 9));
        var tail = series.PercentileInterval(0.95, 0.95);
        Assert.Equal(7, tail.LowerRank);
        Assert.Equal(9, tail.UpperRank);
        Assert.Equal(7m, tail.Lower);
        Assert.Equal(9m, tail.Upper);
        Assert.True(tail.ReachedCoverage);
        Assert.True(tail.Coverage + 1e-12 >= 0.95);
    }

    [Fact]
    public void PR04_003_when_gamma_cannot_be_met_range_is_the_sample()
    {
        var series = NumericSeries.From(Enumerable.Range(1, 9));
        var low = series.PercentileInterval(0.25, 0.95);
        Assert.False(low.ReachedCoverage);
        Assert.Equal(PercentileInterval.SampleRangeMethod, low.Method);
        Assert.Equal(1, low.LowerRank);
        Assert.Equal(9, low.UpperRank);
        Assert.Equal(1m, low.Lower);
        Assert.Equal(9m, low.Upper);
    }

    [Fact]
    public void PR04_005_one_to_nine_is_a_finite_density()
    {
        var pts = NumericSeries.From(Enumerable.Range(1, 9)).PdfPoints();
        Assert.Equal(KernelDensity.DefaultCount, pts.Count);
        Assert.True(((IList<DensityPoint>)pts).IsReadOnly);
        Assert.All(pts, p =>
        {
            Assert.True(double.IsFinite(p.X));
            Assert.True(double.IsFinite(p.Y));
            Assert.True(p.Y >= 0);
        });

        double area = 0;
        for (var i = 1; i < pts.Count; i++)
            area += 0.5 * (pts[i].Y + pts[i - 1].Y) * (pts[i].X - pts[i - 1].X);
        Assert.InRange(area, 0.85, 1.05);
    }

    [Fact]
    public void PR04_005_constant_is_empty_and_count_is_guarded()
    {
        var empty = NumericSeries.From([5, 5, 5]).PdfPoints();
        Assert.Empty(empty);

        var series = NumericSeries.From(Enumerable.Range(1, 9));
        Assert.Throws<ArgumentOutOfRangeException>(() => series.PdfPoints(0));
        Assert.Throws<ArgumentOutOfRangeException>(() => series.PdfPoints(KernelDensity.MaxCount + 1));
    }
}
