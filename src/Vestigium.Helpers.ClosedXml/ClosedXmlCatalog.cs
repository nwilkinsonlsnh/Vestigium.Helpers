using Vestigium.Logging;

namespace Vestigium.Helpers.ClosedXml;

public static class ClosedXmlCatalog
{
    public const string AppId = "ClosedXml";
    public const string Category = "Helpers";

    public static class Subcategories
    {
        public const string Probe = "Probe";
        public const string Session = "Session";
        public const string Sheet = "Sheet";
        public const string Chart = "Chart";
    }

    public static void Register(VestigiumLoggerOptions cfg)
    {
        ArgumentNullException.ThrowIfNull(cfg);
        var taxonomy = new VestigiumTaxonomy();
        taxonomy.Register(Category, AppId);
        taxonomy.Register(Category, Subcategories.Probe, Subcategories.Session, Subcategories.Sheet, Subcategories.Chart);
        cfg.RegisterTaxonomy(taxonomy);
        foreach (var row in Rows)
        {
            cfg.RegisterEvent(row.Name, row.FullName, Category, row.Subcategory, row.EventId, row.Severity, row.Description);
        }
    }

    internal static readonly CatalogRow[] Rows =
    [
        Row(ClosedXmlEvents.ProbeEnter, "ProbeEnter", Subcategories.Probe, "Debug", "enter Probe"),
        Row(ClosedXmlEvents.ProbeComplete, "ProbeComplete", Subcategories.Probe, "Information", "probe complete"),
        Row(ClosedXmlEvents.SessionEnter, "SessionEnter", Subcategories.Session, "Debug", "enter session"),
        Row(ClosedXmlEvents.SessionCreated, "SessionCreated", Subcategories.Session, "Information", "session created"),
        Row(ClosedXmlEvents.SessionOpened, "SessionOpened", Subcategories.Session, "Information", "session opened"),
        Row(ClosedXmlEvents.SessionSaved, "SessionSaved", Subcategories.Session, "Information", "session saved"),
        Row(ClosedXmlEvents.SessionMerged, "SessionMerged", Subcategories.Session, "Information", "session merged"),
        Row(ClosedXmlEvents.SessionRejected, "SessionRejected", Subcategories.Session, "Error", "rejected session"),
        Row(ClosedXmlEvents.SessionThrown, "SessionThrown", Subcategories.Session, "Error", "unexpected failure"),
        Row(ClosedXmlEvents.SheetEnter, "SheetEnter", Subcategories.Sheet, "Debug", "enter sheet"),
        Row(ClosedXmlEvents.SheetWrote, "SheetWrote", Subcategories.Sheet, "Debug", "sheet wrote"),
        Row(ClosedXmlEvents.SheetRejected, "SheetRejected", Subcategories.Sheet, "Error", "rejected sheet"),
        Row(ClosedXmlEvents.ChartQueued, "ChartQueued", Subcategories.Chart, "Information", "chart queued"),
        Row(ClosedXmlEvents.ChartRejected, "ChartRejected", Subcategories.Chart, "Error", "rejected chart"),
        Row(ClosedXmlEvents.CellNeutralized, "CellNeutralized", Subcategories.Sheet, "Warning", "neutralized formula-like text"),
        Row(ClosedXmlEvents.CellRejectedNonFinite, "CellRejectedNonFinite", Subcategories.Sheet, "Error", "rejected non-finite number"),
        Row(ClosedXmlEvents.WriteSeriesComplete, "WriteSeriesComplete", Subcategories.Session, "Information", "series workbook written"),
        Row(ClosedXmlEvents.ChartsEmbedded, "ChartsEmbedded", Subcategories.Chart, "Information", "charts embedded"),
        Row(ClosedXmlEvents.ChartPackFailed, "ChartPackFailed", Subcategories.Chart, "Error", "chart pack failed"),
    ];

    private static CatalogRow Row(int eventId, string name, string subcategory, string severity, string description)
        => new(eventId, name, $"Vestigium.Helpers.ClosedXml.Events.{name}", subcategory, severity, description);

    internal readonly record struct CatalogRow(
        int EventId, string Name, string FullName, string Subcategory, string Severity, string Description);
}
