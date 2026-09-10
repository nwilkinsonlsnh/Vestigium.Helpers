using Vestigium.Helpers;
using Vestigium.Helpers.Analytics;

namespace Vestigium.Helpers.Network;

internal static class PercentileBillEngine
{
    public static PercentileBill FromSamples(IEnumerable<decimal> samplesBitsPerSecond, double percentile = 0.95)
    {
        var list = samplesBitsPerSecond?.ToList() ?? throw new ArgumentNullException(nameof(samplesBitsPerSecond));
        if (list.Count == 0)
        {
            HelperLog.Reject(HelperLog.AppIds.Network, "Bandwidth", nameof(FromSamples), "empty samples");
            throw new ArgumentException("P95 billing needs at least one sample.", nameof(samplesBitsPerSecond));
        }

        var series = NumericSeries.FromDecimal(list, "bandwidth-samples");
        return FromSeries(series, percentile);
    }

    public static PercentileBill FromSeries(NumericSeries series, double percentile = 0.95)
    {
        HelperGuard.NotNull(series, nameof(series));
        if (series.Count == 0)
        {
            HelperLog.Reject(HelperLog.AppIds.Network, "Bandwidth", nameof(FromSeries), "empty series");
            throw new ArgumentException("P95 billing needs at least one sample.", nameof(series));
        }

        if (percentile is <= 0 or >= 1)
        {
            HelperLog.Reject(HelperLog.AppIds.Network, "Bandwidth", nameof(FromSeries), $"p={percentile}");
            throw new ArgumentOutOfRangeException(nameof(percentile), "Percentile must be between 0 and 1.");
        }

        var bitsPerSecond = series.Full.Percentile(percentile);
        var rate = BandwidthEngine.From(bitsPerSecond, DataUnit.Bit);
        var display = BandwidthEngine.Convert(rate, DataUnit.Mb);
        var day = BandwidthEngine.VolumeFromRate(display, BandwidthBasis.Day);
        var month = BandwidthEngine.VolumeFromRate(display, BandwidthBasis.Days30);
        var year = BandwidthEngine.VolumeFromRate(display, BandwidthBasis.Year365);
        NetworkLog.Success("Bandwidth", $"p{percentile:0.##} n={series.Count} bits/s={bitsPerSecond}");
        return new PercentileBill(
            percentile,
            series.Count,
            display,
            day,
            month,
            year,
            series.SeriesId,
            $"P{percentile * 100:0} = {display.Display}/s over n={series.Count}; 30-day {month.Volume.Display}");
    }
}
