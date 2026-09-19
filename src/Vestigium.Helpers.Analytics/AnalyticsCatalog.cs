using Vestigium.Logging;

namespace Vestigium.Helpers.Analytics;

/// <summary>
/// Registers the Analytics custom catalog (EVENTID 10500–10615) during
/// <see cref="VestigiumLogger.Initialize"/>. Does not initialize the host
/// and does not call <see cref="VestigiumLogger.LoadCustomCatalog"/>.
/// </summary>
public static class AnalyticsCatalog
{
    public const string AppId = "Analytics";
    public const string Category = "Helpers";

    public static class Subcategories
    {
        public const string Probe = "Probe";
        public const string Series = "Series";
        public const string Confidence = "Confidence";
        public const string Limits = "Limits";
    }

    public static void Register(VestigiumLoggerOptions cfg)
    {
        ArgumentNullException.ThrowIfNull(cfg);

        var taxonomy = new VestigiumTaxonomy();
        taxonomy.Register(Category, AppId);
        taxonomy.Register(Category, Subcategories.Probe, Subcategories.Series, Subcategories.Confidence, Subcategories.Limits);
        cfg.RegisterTaxonomy(taxonomy);

        foreach (var row in Rows)
        {
            cfg.RegisterEvent(
                row.Name,
                row.FullName,
                Category,
                row.Subcategory,
                row.EventId,
                row.Severity,
                row.Description);
        }
    }

    internal static readonly CatalogRow[] Rows =
    [
        Row(AnalyticsEvents.ProbeEnter, "ProbeEnter", Subcategories.Probe, "Debug", "enter Probe"),
        Row(AnalyticsEvents.ProbeComplete, "ProbeComplete", Subcategories.Probe, "Information", "probe complete"),
        Row(AnalyticsEvents.SeriesEnter, "SeriesEnter", Subcategories.Series, "Debug", "enter series"),
        Row(AnalyticsEvents.SeriesConstructed, "SeriesConstructed", Subcategories.Series, "Information", "constructed"),
        Row(AnalyticsEvents.SeriesRejectedEmpty, "SeriesRejectedEmpty", Subcategories.Series, "Error", "rejected empty series"),
        Row(AnalyticsEvents.SeriesRejectedNonFinite, "SeriesRejectedNonFinite", Subcategories.Series, "Error", "rejected non-finite value"),
        Row(AnalyticsEvents.SeriesRejectedOverflow, "SeriesRejectedOverflow", Subcategories.Series, "Error", "rejected overflow"),
        Row(AnalyticsEvents.SeriesRejectedEmptySlice, "SeriesRejectedEmptySlice", Subcategories.Series, "Error", "rejected empty slice"),
        Row(AnalyticsEvents.SeriesRejectedEmptyObservations, "SeriesRejectedEmptyObservations", Subcategories.Series, "Error", "rejected empty observations"),
        Row(AnalyticsEvents.SeriesThrown, "SeriesThrown", Subcategories.Series, "Error", "unexpected failure"),
        Row(AnalyticsEvents.ConfidenceEnter, "ConfidenceEnter", Subcategories.Confidence, "Debug", "enter confidence"),
        Row(AnalyticsEvents.ConfidenceComputed, "ConfidenceComputed", Subcategories.Confidence, "Information", "confidence computed"),
        Row(AnalyticsEvents.ConfidenceRejectedLevel, "ConfidenceRejectedLevel", Subcategories.Confidence, "Error", "rejected confidence level"),
        Row(AnalyticsEvents.ConfidenceRejectedPopulation, "ConfidenceRejectedPopulation", Subcategories.Confidence, "Error", "rejected population size"),
        Row(AnalyticsEvents.ConfidenceRejectedMargin, "ConfidenceRejectedMargin", Subcategories.Confidence, "Error", "rejected target margin"),
        Row(AnalyticsEvents.ConfidenceThrown, "ConfidenceThrown", Subcategories.Confidence, "Error", "unexpected failure"),
        Row(AnalyticsEvents.LimitsEnter, "LimitsEnter", Subcategories.Limits, "Debug", "enter limits"),
        Row(AnalyticsEvents.LimitsComputed, "LimitsComputed", Subcategories.Limits, "Information", "limits computed"),
        Row(AnalyticsEvents.LimitsCallerSupplied, "LimitsCallerSupplied", Subcategories.Limits, "Information", "caller-supplied limits"),
        Row(AnalyticsEvents.LimitsOutOfControl, "LimitsOutOfControl", Subcategories.Limits, "Warning", "out-of-control points"),
        Row(AnalyticsEvents.LimitsRejected, "LimitsRejected", Subcategories.Limits, "Error", "rejected limits"),
        Row(AnalyticsEvents.LimitsThrown, "LimitsThrown", Subcategories.Limits, "Error", "unexpected failure"),
        Row(AnalyticsEvents.PercentileRejectedEmpty, "PercentileRejectedEmpty", Subcategories.Series, "Error", "rejected empty percentile"),
        Row(AnalyticsEvents.PercentileRejectedP, "PercentileRejectedP", Subcategories.Series, "Error", "rejected percentile p"),
    ];

    private static CatalogRow Row(int eventId, string name, string subcategory, string severity, string description)
        => new(eventId, name, $"Vestigium.Helpers.Analytics.Events.{name}", subcategory, severity, description);

    internal readonly record struct CatalogRow(
        int EventId,
        string Name,
        string FullName,
        string Subcategory,
        string Severity,
        string Description);
}
