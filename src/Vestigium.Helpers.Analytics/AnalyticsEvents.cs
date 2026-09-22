namespace Vestigium.Helpers.Analytics;

/// <summary>
/// Custom catalog block 10500–10999 (count by 5). Hosts register these via
/// <see cref="AnalyticsCatalog.Register"/>.
/// </summary>
public static class AnalyticsEvents
{
    /// <summary>First EVENTID reserved for Analytics.</summary>
    public const int BlockStart = 10500;
    /// <summary>Last EVENTID reserved for Analytics.</summary>
    public const int BlockEnd = 10999;

    /// <summary>Enter <see cref="AnalyticsHelper.Probe"/>.</summary>
    public const int ProbeEnter = 10500;
    /// <summary>Probe finished.</summary>
    public const int ProbeComplete = 10505;
    /// <summary>Enter series construction or slice.</summary>
    public const int SeriesEnter = 10510;
    /// <summary>Series snapshot constructed.</summary>
    public const int SeriesConstructed = 10515;
    /// <summary>Empty value sequence rejected.</summary>
    public const int SeriesRejectedEmpty = 10520;
    /// <summary>Non-finite value rejected.</summary>
    public const int SeriesRejectedNonFinite = 10525;
    /// <summary>Decimal overflow rejected.</summary>
    public const int SeriesRejectedOverflow = 10530;
    /// <summary>Time slice produced zero points.</summary>
    public const int SeriesRejectedEmptySlice = 10535;
    /// <summary>Empty observation sequence rejected.</summary>
    public const int SeriesRejectedEmptyObservations = 10540;
    /// <summary>Unexpected failure on the series path.</summary>
    public const int SeriesThrown = 10545;
    /// <summary>Enter a confidence calculation.</summary>
    public const int ConfidenceEnter = 10550;
    /// <summary>Confidence report computed.</summary>
    public const int ConfidenceComputed = 10555;
    /// <summary>γ outside (0, 1).</summary>
    public const int ConfidenceRejectedLevel = 10560;
    /// <summary>Population size illegal (N &lt; 1 or n &gt; N).</summary>
    public const int ConfidenceRejectedPopulation = 10565;
    /// <summary>Planning margin was not positive.</summary>
    public const int ConfidenceRejectedMargin = 10570;
    /// <summary>Unexpected failure on the confidence path.</summary>
    public const int ConfidenceThrown = 10575;
    /// <summary>Enter control-limit calculation.</summary>
    public const int LimitsEnter = 10580;
    /// <summary>Control limits computed.</summary>
    public const int LimitsComputed = 10585;
    /// <summary>Caller-supplied fences accepted.</summary>
    public const int LimitsCallerSupplied = 10590;
    /// <summary>One or more points sit outside UCL/LCL.</summary>
    public const int LimitsOutOfControl = 10595;
    /// <summary>Limits rejected (n, s, MR, band, k, method).</summary>
    public const int LimitsRejected = 10600;
    /// <summary>Unexpected failure on the limits path.</summary>
    public const int LimitsThrown = 10605;
    /// <summary>Percentile requested on an empty slice.</summary>
    public const int PercentileRejectedEmpty = 10610;
    /// <summary>Percentile p outside [0, 1].</summary>
    public const int PercentileRejectedP = 10615;
}
