namespace Vestigium.Helpers.Analytics;

/// <summary>
/// Custom catalog block 10500–10999 (count by 5). Hosts register these via
/// <see cref="AnalyticsCatalog.Register"/>.
/// </summary>
public static class AnalyticsEvents
{
    public const int BlockStart = 10500;
    public const int BlockEnd = 10999;

    public const int ProbeEnter = 10500;
    public const int ProbeComplete = 10505;
    public const int SeriesEnter = 10510;
    public const int SeriesConstructed = 10515;
    public const int SeriesRejectedEmpty = 10520;
    public const int SeriesRejectedNonFinite = 10525;
    public const int SeriesRejectedOverflow = 10530;
    public const int SeriesRejectedEmptySlice = 10535;
    public const int SeriesRejectedEmptyObservations = 10540;
    public const int SeriesThrown = 10545;
    public const int ConfidenceEnter = 10550;
    public const int ConfidenceComputed = 10555;
    public const int ConfidenceRejectedLevel = 10560;
    public const int ConfidenceRejectedPopulation = 10565;
    public const int ConfidenceRejectedMargin = 10570;
    public const int ConfidenceThrown = 10575;
    public const int LimitsEnter = 10580;
    public const int LimitsComputed = 10585;
    public const int LimitsCallerSupplied = 10590;
    public const int LimitsOutOfControl = 10595;
    public const int LimitsRejected = 10600;
    public const int LimitsThrown = 10605;
    public const int PercentileRejectedEmpty = 10610;
    public const int PercentileRejectedP = 10615;
}
