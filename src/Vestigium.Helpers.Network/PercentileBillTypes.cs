using Vestigium.Helpers.Analytics;

namespace Vestigium.Helpers.Network;

public sealed record PercentileBill(
    double Percentile,
    int SampleCount,
    BandwidthAmount Rate,
    PeriodVolume Day,
    PeriodVolume Days30,
    PeriodVolume Year365,
    string? SeriesId,
    string Summary);
