using Vestigium.Helpers.Analytics;

namespace Vestigium.Helpers.Tests;

public sealed class AnalyticsPR04NelsonTests
{
    [Fact]
    public void PR04_007_one_to_nine_is_nelson_five()
    {
        var report = NumericSeries.From(Enumerable.Range(1, 9)).RunRules();
        var trend = Assert.Single(report.Hits.Where(h => h.Rule == WesternElectricRule.SixIncreasingOrDecreasing));
        Assert.Equal(Enumerable.Range(0, 9), trend.Indexes);
        Assert.True(((IList<int>)trend.Indexes).IsReadOnly);
        Assert.True(((IList<int>)report.AllIndexes).IsReadOnly);
    }

    [Fact]
    public void PR04_007_eight_outside_zone_c()
    {
        var values = Enumerable.Repeat(10, 20).Concat(Enumerable.Repeat(30, 8)).ToArray();
        var report = NumericSeries.From(values).RunRules();
        var rule8 = Assert.Single(report.Hits.Where(h => h.Rule == WesternElectricRule.EightOutsideZoneC));
        Assert.Equal(Enumerable.Range(20, 8), rule8.Indexes);
    }

    [Fact]
    public void PR04_007_wiggle_has_no_western_electric_one_through_four()
    {
        var report = NumericSeries.From(new[] { 5, 4, 6, 5, 4, 6, 5, 4, 6 }).RunRules();
        Assert.DoesNotContain(report.Hits, h => (int)h.Rule <= 4);
    }
}
