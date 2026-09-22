using Vestigium.Helpers.Analytics;

namespace Vestigium.Helpers.Tests;

public sealed class AnalyticsPR03CompareTests
{
    [Fact]
    public void PR03_007_equal_counts_pair_and_welch_is_one_when_identical()
    {
        var a = NumericSeries.From(Enumerable.Range(1, 9));
        var b = NumericSeries.From(Enumerable.Range(1, 9));
        var cmp = a.Compare(b);

        Assert.Equal(9, cmp.Count1);
        Assert.Equal(9, cmp.Count2);
        Assert.Equal(0d, cmp.MeanDelta);
        Assert.NotNull(cmp.Paired);
        Assert.Equal(9, cmp.Paired!.Count);
        Assert.All(cmp.Paired.Values, v => Assert.Equal(0m, v));
        Assert.Equal(0d, cmp.WelchT);
        Assert.Equal(1d, cmp.WelchTwoSidedP!.Value, 10);
        Assert.Same(a, a);
    }

    [Fact]
    public void PR03_007_unequal_counts_have_no_pair_and_a_welch_p()
    {
        var a = NumericSeries.From(Enumerable.Range(1, 9));
        var b = NumericSeries.From(Enumerable.Range(1, 5));
        var cmp = a.Compare(b);

        Assert.Null(cmp.Paired);
        Assert.True(cmp.MeanDelta > 0);
        Assert.NotNull(cmp.WelchT);
        Assert.NotNull(cmp.WelchTwoSidedP);
        Assert.InRange(cmp.WelchTwoSidedP!.Value, 0d, 1d);
        Assert.Throws<ArgumentNullException>(() => a.Compare(null!));
    }
}
