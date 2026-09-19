using Vestigium.Helpers.Analytics;

namespace Vestigium.Helpers.Tests;

public sealed class AnalyticsPR03Tests
{
    [Fact]
    public void PR03_001_from_does_not_score()
    {
        var spec = SpecLimits.From(lower: 0, upper: 30);
        Assert.Equal(0d, spec.Lower);
        Assert.Equal(30d, spec.Upper);
        Assert.Equal(0, spec.OutsideCount);
        Assert.Empty(spec.OutsideIndexes);
    }

    [Fact]
    public void PR03_001_against_flags_strict_outside()
    {
        var spec = SpecLimits.From(0, 30);
        var scored = spec.Against([12m, 12m, 40m, 0m, 30m]);

        Assert.Equal(new[] { 2 }, scored.OutsideIndexes);
        Assert.Equal(1, scored.OutsideCount);
        Assert.True(scored.IsOutside(40));
        Assert.False(scored.IsOutside(0));
        Assert.False(scored.IsOutside(30));
        Assert.Equal(0, spec.OutsideCount);
    }

    [Fact]
    public void PR03_001_one_sided_and_empty_input()
    {
        var usl = SpecLimits.From(upper: 10).Against([9m, 11m]);
        Assert.Equal(new[] { 1 }, usl.OutsideIndexes);
        Assert.Null(usl.Lower);

        var lsl = SpecLimits.From(lower: 5).Against([4m, 5m]);
        Assert.Equal(new[] { 0 }, lsl.OutsideIndexes);
        Assert.Null(lsl.Upper);

        var empty = SpecLimits.From(0, 1).Against([]);
        Assert.Equal(0, empty.OutsideCount);
        Assert.Empty(empty.OutsideIndexes);

        var missing = SpecLimits.From(0, 1).Against(null);
        Assert.Equal(0, missing.OutsideCount);
    }

    [Fact]
    public void PR03_001_indexes_are_frozen()
    {
        var scored = SpecLimits.From(0, 10).Against([1m, 99m]);
        Assert.True(((IList<int>)scored.OutsideIndexes).IsReadOnly);
        Assert.Throws<NotSupportedException>(() => ((IList<int>)scored.OutsideIndexes)[0] = 99);
    }

    [Fact]
    public void PR03_001_rejects_bad_specs()
    {
        Assert.Throws<ArgumentException>(() => SpecLimits.From());
        Assert.Throws<ArgumentException>(() => SpecLimits.From(10, 10));
        Assert.Throws<ArgumentException>(() => SpecLimits.From(10, 5));
        Assert.Throws<ArgumentOutOfRangeException>(() => SpecLimits.From(double.NaN, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => SpecLimits.From(0, double.PositiveInfinity));
    }
}
