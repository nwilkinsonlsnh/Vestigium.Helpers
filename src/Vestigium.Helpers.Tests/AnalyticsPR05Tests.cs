using Vestigium.Helpers.Analytics;

namespace Vestigium.Helpers.Tests;

public sealed class AnalyticsPR05Tests
{
    [Fact]
    public void PR05_002_mean_interval_narrows_when_gamma_drops()
    {
        var series = NumericSeries.From(Enumerable.Range(1, 9));
        var wide = series.Confidence(0.95).Mean;
        var tight = series.Confidence(0.80).Mean;

        Assert.True(wide.IsDefined);
        Assert.True(tight.IsDefined);
        Assert.Equal(wide.Estimate, tight.Estimate);
        Assert.True(wide.Upper!.Value - wide.Lower!.Value > tight.Upper!.Value - tight.Lower!.Value);
    }

    [Fact]
    public void PR05_002_undefined_mean_interval_is_not_drawn_as_numbers()
    {
        var report = NumericSeries.From([5]).Confidence(0.95);
        Assert.False(report.Mean.IsDefined);
    }
}
