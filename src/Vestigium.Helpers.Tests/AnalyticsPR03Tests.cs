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

    [Fact]
    public void PR03_002_pp_ppk_on_one_to_nine()
    {
        var series = NumericSeries.From(Enumerable.Range(1, 9));
        var spec = SpecLimits.From(0, 15);
        var cap = series.Capability(spec);
        var s = series.Full.StdDev!.Value;

        Assert.Equal(5d, cap.Mean);
        Assert.Equal((15d - 0d) / (6d * s), cap.Pp!.Value, 10);
        Assert.Equal((5d - 0d) / (3d * s), cap.Ppl!.Value, 10);
        Assert.Equal((15d - 5d) / (3d * s), cap.Ppu!.Value, 10);
        Assert.Equal(cap.Ppl, cap.Ppk);
        Assert.Null(cap.Cp);
        Assert.Null(cap.Cpk);
        Assert.Empty(cap.Spec.OutsideIndexes);
    }

    [Fact]
    public void PR03_002_one_sided_and_constant_are_null_not_throw()
    {
        var series = NumericSeries.From(Enumerable.Range(1, 9));
        var usl = series.Capability(SpecLimits.From(upper: 15));
        Assert.Null(usl.Pp);
        Assert.Null(usl.Ppl);
        Assert.NotNull(usl.Ppu);
        Assert.Equal(usl.Ppu, usl.Ppk);

        var constant = NumericSeries.From(new[] { 5, 5, 5 }).Capability(SpecLimits.From(0, 10));
        Assert.Null(constant.Pp);
        Assert.Null(constant.Ppk);
        Assert.Equal(5d, constant.Mean);
    }
}
