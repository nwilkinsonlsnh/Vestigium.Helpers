using Vestigium.Helpers.Analytics;

namespace Vestigium.Helpers.Tests;

public sealed class AnalyticsPR05CloseTests
{
    [Fact]
    public void PR05_007_percentile_interval_is_defined_on_one_to_nine()
    {
        var series = NumericSeries.From(Enumerable.Range(1, 9));
        var ci = series.PercentileInterval(0.95);
        Assert.True(ci.Upper >= ci.Lower);
        Assert.InRange(ci.P, 0.94, 0.96);
        Assert.False(string.IsNullOrWhiteSpace(ci.Method));
    }

    [Fact]
    public void PR05_007_kde_points_are_finite()
    {
        var pts = NumericSeries.From(Enumerable.Range(1, 9)).PdfPoints();
        Assert.NotEmpty(pts);
        Assert.All(pts, p =>
        {
            Assert.True(double.IsFinite(p.X));
            Assert.True(double.IsFinite(p.Y));
            Assert.True(p.Y >= 0);
        });
    }
}
