using Vestigium.Logging;

namespace Vestigium.Helpers.Csv;

public static class CsvCatalog
{
    public const string AppId = "Csv";
    public const string Category = "Helpers";

    public static class Subcategories
    {
        public const string Probe = "Probe";
        public const string Session = "Session";
        public const string Parse = "Parse";
        public const string Series = "Series";
    }

    public static void Register(VestigiumLoggerOptions cfg)
    {
        ArgumentNullException.ThrowIfNull(cfg);
        var taxonomy = new VestigiumTaxonomy();
        taxonomy.Register(Category, AppId);
        taxonomy.Register(Category, Subcategories.Probe, Subcategories.Session, Subcategories.Parse, Subcategories.Series);
        cfg.RegisterTaxonomy(taxonomy);
        foreach (var row in Rows)
        {
            cfg.RegisterEvent(row.Name, row.FullName, Category, row.Subcategory, row.EventId, row.Severity, row.Description);
        }
    }

    internal static readonly CatalogRow[] Rows =
    [
        Row(CsvEvents.ProbeEnter, "ProbeEnter", Subcategories.Probe, "Debug", "enter Probe"),
        Row(CsvEvents.ProbeComplete, "ProbeComplete", Subcategories.Probe, "Information", "probe complete"),
        Row(CsvEvents.SessionEnter, "SessionEnter", Subcategories.Session, "Debug", "enter session"),
        Row(CsvEvents.SessionCreated, "SessionCreated", Subcategories.Session, "Information", "session created"),
        Row(CsvEvents.SessionOpened, "SessionOpened", Subcategories.Session, "Information", "session opened"),
        Row(CsvEvents.SessionSaved, "SessionSaved", Subcategories.Session, "Information", "session saved"),
        Row(CsvEvents.SessionRejected, "SessionRejected", Subcategories.Session, "Error", "rejected session"),
        Row(CsvEvents.SessionThrown, "SessionThrown", Subcategories.Session, "Error", "unexpected failure"),
        Row(CsvEvents.ParseRejected, "ParseRejected", Subcategories.Parse, "Error", "rejected parse"),
        Row(CsvEvents.WriteRejected, "WriteRejected", Subcategories.Session, "Error", "rejected write"),
        Row(CsvEvents.CellNeutralized, "CellNeutralized", Subcategories.Session, "Warning", "neutralized formula-like text"),
        Row(CsvEvents.CellRejectedNonFinite, "CellRejectedNonFinite", Subcategories.Session, "Error", "rejected non-finite number"),
        Row(CsvEvents.WriteSeriesComplete, "WriteSeriesComplete", Subcategories.Series, "Information", "series csv written"),
    ];

    private static CatalogRow Row(int eventId, string name, string subcategory, string severity, string description)
        => new(eventId, name, $"Vestigium.Helpers.Csv.Events.{name}", subcategory, severity, description);

    internal readonly record struct CatalogRow(
        int EventId, string Name, string FullName, string Subcategory, string Severity, string Description);
}
