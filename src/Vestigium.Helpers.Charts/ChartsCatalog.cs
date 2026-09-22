using Vestigium.Logging;

namespace Vestigium.Helpers.Charts;

/// <summary>
/// Registers Charts custom catalog (EVENTID 16500–16545) during Initialize.
/// Does not initialize the host and does not call LoadCustomCatalog.
/// </summary>
public static class ChartsCatalog
{
    public const string AppId = "Charts";
    public const string Category = "Helpers";

    public static class Subcategories
    {
        public const string Probe = "Probe";
        public const string Chart = "Chart";
    }

    public static void Register(VestigiumLoggerOptions cfg)
    {
        ArgumentNullException.ThrowIfNull(cfg);
        var taxonomy = new VestigiumTaxonomy();
        taxonomy.Register(Category, AppId);
        taxonomy.Register(Category, Subcategories.Probe, Subcategories.Chart);
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
        Row(ChartsEvents.ProbeEnter, "ProbeEnter", Subcategories.Probe, "Debug", "enter Probe"),
        Row(ChartsEvents.ProbeComplete, "ProbeComplete", Subcategories.Probe, "Information", "probe complete"),
        Row(ChartsEvents.ChartEnter, "ChartEnter", Subcategories.Chart, "Debug", "enter chart"),
        Row(ChartsEvents.ChartBuilt, "ChartBuilt", Subcategories.Chart, "Information", "chart built"),
        Row(ChartsEvents.ChartSaved, "ChartSaved", Subcategories.Chart, "Information", "chart saved"),
        Row(ChartsEvents.ChartRejectedEmpty, "ChartRejectedEmpty", Subcategories.Chart, "Error", "rejected empty chart input"),
        Row(ChartsEvents.ChartRejectedXy, "ChartRejectedXy", Subcategories.Chart, "Error", "rejected x/y length mismatch"),
        Row(ChartsEvents.ChartRejectedLimits, "ChartRejectedLimits", Subcategories.Chart, "Error", "rejected malformed limits"),
        Row(ChartsEvents.ChartRejectedBlankPath, "ChartRejectedBlankPath", Subcategories.Chart, "Error", "rejected blank path"),
        Row(ChartsEvents.ChartThrown, "ChartThrown", Subcategories.Chart, "Error", "unexpected failure"),
    ];

    private static CatalogRow Row(int eventId, string name, string subcategory, string severity, string description)
        => new(eventId, name, $"Vestigium.Helpers.Charts.Events.{name}", subcategory, severity, description);

    internal readonly record struct CatalogRow(
        int EventId, string Name, string FullName, string Subcategory, string Severity, string Description);
}
