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

        Assert.Equal([2], scored.OutsideIndexes);
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
        Assert.Equal([1], usl.OutsideIndexes);
        Assert.Null(usl.Lower);

        var lsl = SpecLimits.From(lower: 5).Against([4m, 5m]);
        Assert.Equal([0], lsl.OutsideIndexes);
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

        var constant = NumericSeries.From([5, 5, 5]).Capability(SpecLimits.From(0, 10));
        Assert.Null(constant.Pp);
        Assert.Null(constant.Ppk);
        Assert.Null(constant.Cp);
        Assert.Null(constant.Cpk);
        Assert.Equal(5d, constant.Mean);
    }

    [Fact]
    public void PR03_003_cp_cpk_use_mr_over_d2_on_full()
    {
        var series = NumericSeries.From(Enumerable.Range(1, 9));
        var cap = series.Capability(SpecLimits.From(0, 15));
        var sigmaW = 1d / ControlLimits.D2Span2;

        Assert.Equal(1d, cap.MovingRangeBar);
        Assert.Equal(sigmaW, cap.WithinSigma!.Value, 12);
        Assert.Equal(15d / (6d * sigmaW), cap.Cp!.Value, 10);
        Assert.Equal(5d / (3d * sigmaW), cap.Cpl!.Value, 10);
        Assert.Equal(10d / (3d * sigmaW), cap.Cpu!.Value, 10);
        Assert.Equal(cap.Cpl, cap.Cpk);
    }

    [Fact]
    public void PR03_003_cp_is_null_on_value_bands()
    {
        var series = NumericSeries.From(Enumerable.Range(1, 9));
        var q4 = series.Q4.Capability(SpecLimits.From(0, 15));
        Assert.NotNull(q4.Ppk);
        Assert.Null(q4.Cp);
        Assert.Null(q4.Cpk);
        Assert.Null(q4.MovingRangeBar);
        Assert.Null(q4.WithinSigma);
    }

    [Fact]
    public void PR03_004_one_sided_cp_matches_the_present_side()
    {
        var series = NumericSeries.From(Enumerable.Range(1, 9));
        var sigmaW = 1d / ControlLimits.D2Span2;

        var usl = series.Capability(SpecLimits.From(upper: 15));
        Assert.Null(usl.Cp);
        Assert.Null(usl.Cpl);
        Assert.Equal(10d / (3d * sigmaW), usl.Cpu!.Value, 10);
        Assert.Equal(usl.Cpu, usl.Cpk);

        var lsl = series.Capability(SpecLimits.From(lower: 0));
        Assert.Null(lsl.Cp);
        Assert.Null(lsl.Cpu);
        Assert.Equal(5d / (3d * sigmaW), lsl.Cpl!.Value, 10);
        Assert.Equal(lsl.Cpl, lsl.Cpk);
    }

    [Fact]
    public void PR03_004_capability_carries_frozen_outside_spec_indexes()
    {
        var series = NumericSeries.From([1, 2, 3, 4, 5, 6, 7, 8, 40]);
        var cap = series.Capability(SpecLimits.From(0, 15));
        Assert.Equal([8], cap.Spec.OutsideIndexes);
        Assert.Equal(1, cap.Spec.OutsideCount);
        Assert.True(((IList<int>)cap.Spec.OutsideIndexes).IsReadOnly);
        Assert.Throws<ArgumentNullException>(() => series.Capability(null!));
    }

    [Fact]
    public void PR03_005_value_band_and_caller_supplied_are_rejected()
    {
        var series = NumericSeries.From(Enumerable.Range(1, 9));
        var band = Assert.Throws<InvalidOperationException>(() => series.Q4.RunRules());
        Assert.Equal(RunRuleReport.RequiresFull, band.Message);
        Assert.Throws<ArgumentException>(() => series.RunRules(ControlLimitMethod.CallerSupplied));
    }

    [Fact]
    public void PR03_005_quiet_series_has_no_western_electric_hits()
    {
        var report = NumericSeries.From([5, 4, 6, 5, 4, 6, 5, 4, 6]).RunRules();
        Assert.DoesNotContain(report.Hits, h => (int)h.Rule <= 4);
        Assert.Equal(ControlLimitMethod.MeanPlusKSigma, report.Limits.Method);
    }

    [Fact]
    public void PR03_006_spike_fires_rule_one()
    {
        var values = Enumerable.Repeat(5, 30).Append(1000).ToArray();
        var report = NumericSeries.From(values).RunRules();
        var rule1 = Assert.Single(report.Hits.Where(h => h.Rule == WesternElectricRule.PointBeyondThreeSigma));
        Assert.Contains(30, rule1.Indexes);
        Assert.True(((IList<int>)rule1.Indexes).IsReadOnly);
        Assert.True(((IList<int>)report.AllIndexes).IsReadOnly);
        Assert.Throws<NotSupportedException>(() => ((IList<int>)report.AllIndexes)[0] = 99);
    }

    [Fact]
    public void PR03_006_eight_on_one_side()
    {
        var report = NumericSeries.From([10, 10, 10, 10, 10, 10, 10, 10, 0]).RunRules();
        var rule4 = Assert.Single(report.Hits.Where(h => h.Rule == WesternElectricRule.EightOnOneSideOfCenter));
        Assert.Equal(Enumerable.Range(0, 8), rule4.Indexes);
    }

    [Fact]
    public void PR03_006_constant_series_refuses_like_limits()
    {
        var ex = Assert.Throws<InvalidOperationException>(() =>
            NumericSeries.From([5, 5, 5]).RunRules());
        Assert.Contains("standard deviation", ex.Message, StringComparison.OrdinalIgnoreCase);
    }
}
