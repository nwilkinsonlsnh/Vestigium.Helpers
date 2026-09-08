using Vestigium.Helpers.Analytics;

namespace Vestigium.Helpers.Tests;

public sealed class ControlLimitsTests
{
    [Fact]
    public void Mean_plus_k_sigma_on_one_to_nine()
    {
        var series = NumericSeries.From(new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9 }, "odd");
        var limits = series.ControlLimits();

        Assert.Equal(ControlLimitMethod.MeanPlusKSigma, limits.Method);
        Assert.Equal(5d, limits.Center);
        Assert.Equal(3d, limits.K);
        Assert.Equal(3 * series.Full.StdDev!.Value, limits.Upper - limits.Center, 6);
        Assert.Equal(3 * series.Full.StdDev!.Value, limits.Center - limits.Lower, 6);
        Assert.Equal(0, limits.OutOfControlCount);
        Assert.True(limits.Upper > 13.2 && limits.Upper < 13.3);
    }

    [Fact]
    public void Mean_plus_k_sigma_swallows_a_single_spike_on_small_n()
    {
        // s inflates with the spike, so 1000 still sits inside mean ± 3s of {1..9, 1000}.
        var series = NumericSeries.From(new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 1000 });
        var limits = series.ControlLimits(ControlLimitMethod.MeanPlusKSigma, k: 3);
        Assert.Equal(0, limits.OutOfControlCount);
        Assert.False(limits.IsOutOfControl(1000));
        Assert.False(limits.IsOutOfControl(5));
    }

    [Fact]
    public void Mean_plus_k_sigma_flags_a_spike_when_the_process_is_tight()
    {
        var values = Enumerable.Repeat(12m, 25).Append(40.2m).ToArray();
        var series = NumericSeries.From(values, "tight");
        var limits = series.ControlLimits();
        Assert.Equal(1, limits.OutOfControlCount);
        Assert.Equal(new[] { 25 }, limits.OutOfControlIndexes);
        Assert.True(limits.IsOutOfControl(40.2));
        Assert.False(limits.IsOutOfControl(12));
    }

    [Fact]
    public void Moving_range_flags_a_spike_that_three_sigma_swallows()
    {
        var series = NumericSeries.From(
            new[] { 12.4, 11.9, 13.1, 12.0, 18.7, 12.2, 12.5, 11.8, 14.2, 40.2 },
            "rtt-ms");
        var sigma = series.ControlLimits();
        var mr = series.ControlLimits(ControlLimitMethod.MovingRange);
        Assert.Equal(0, sigma.OutOfControlCount);
        Assert.Contains(9, mr.OutOfControlIndexes);
        Assert.True(mr.IsOutOfControl(40.2));
        Assert.False(mr.IsOutOfControl(12.4));
    }

    [Fact]
    public void Moving_range_uses_encounter_order_and_e2()
    {
        var series = NumericSeries.From(new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9 }, "odd");
        var limits = series.ControlLimits(ControlLimitMethod.MovingRange);

        Assert.Equal(ControlLimitMethod.MovingRange, limits.Method);
        Assert.Equal(5d, limits.Center);
        Assert.Equal(1d, limits.MovingRangeBar);
        Assert.Equal(ControlLimits.E2Span2, limits.E2);
        Assert.Equal(8, limits.MovingRanges.Count);
        Assert.All(limits.MovingRanges, mr => Assert.Equal(1d, mr));
        Assert.Equal(5 + ControlLimits.E2Span2, limits.Upper, 10);
        Assert.Equal(5 - ControlLimits.E2Span2, limits.Lower, 10);
        // 1,2 sit below LCL≈2.34; 8,9 sit above UCL≈7.66
        Assert.Equal(4, limits.OutOfControlCount);
        Assert.Equal(new[] { 0, 1, 7, 8 }, limits.OutOfControlIndexes);
    }

    [Fact]
    public void Caller_supplied_does_not_invent_k_sigma()
    {
        var limits = ControlLimits.FromCaller(12, 30, 0);
        Assert.Equal(ControlLimitMethod.CallerSupplied, limits.Method);
        Assert.Equal(12, limits.Center);
        Assert.Equal(30, limits.Upper);
        Assert.Equal(0, limits.Lower);
        Assert.Null(limits.K);
        Assert.Equal(0, limits.OutOfControlCount);
    }

    [Fact]
    public void Floor_clamps_lcl()
    {
        var series = NumericSeries.From(new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9 });
        var limits = series.ControlLimits(ControlLimitMethod.MeanPlusKSigma, k: 3, floor: 0);
        Assert.Equal(0, limits.Lower);
        Assert.Equal(0, limits.Floor);
        Assert.True(limits.Upper > limits.Center);
    }

    [Fact]
    public void Rejects_k_not_positive_and_malformed_caller_band()
    {
        var series = NumericSeries.From(new[] { 1, 2, 3 });
        Assert.Throws<ArgumentOutOfRangeException>(() => series.ControlLimits(k: 0));
        Assert.Throws<ArgumentException>(() => ControlLimits.FromCaller(5, 4, 1));
        Assert.Throws<ArgumentException>(() =>
            series.ControlLimits(ControlLimitMethod.CallerSupplied));
    }

    [Fact]
    public void Constant_series_cannot_form_sigma_or_mr_limits()
    {
        var series = NumericSeries.From(new[] { 5, 5, 5 });
        Assert.Throws<InvalidOperationException>(() => series.ControlLimits());
        Assert.Throws<InvalidOperationException>(() =>
            series.ControlLimits(ControlLimitMethod.MovingRange));
    }
}
