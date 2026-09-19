using Vestigium.Logging;

namespace Vestigium.Helpers.Xml;

public static class XmlCatalog
{
    public const string AppId = "Xml";
    public const string Category = "Helpers";
    public const string Subcategory = "Xml";

    public static void Register(VestigiumLoggerOptions cfg)
    {
        ArgumentNullException.ThrowIfNull(cfg);
        var taxonomy = new VestigiumTaxonomy();
        taxonomy.Register(Category, AppId);
        taxonomy.Register(
            Category,
            "Probe", "Document", "Session", "Multi", "Save", "Query", "Guard",
            "Safety", "Snapshot", "Diff", "Commit");
        cfg.RegisterTaxonomy(taxonomy);
        foreach (var row in Rows)
            cfg.RegisterEvent(row.Name, row.FullName, Category, Subcategory, row.EventId, row.Severity, row.Description);
    }

    internal static readonly CatalogRow[] Rows =
    [
        Row(XmlEvents.ProbeEnter, "ProbeEnter", "Information", "enter Probe"),
        Row(XmlEvents.ProbeComplete, "ProbeComplete", "Information", "probe complete"),
        Row(XmlEvents.OperationEnter, "OperationEnter", "Debug", "enter operation"),
        Row(XmlEvents.OperationComplete, "OperationComplete", "Information", "operation complete"),
        Row(XmlEvents.OperationFailed, "OperationFailed", "Error", "operation failed"),
        Row(XmlEvents.OperationWarning, "OperationWarning", "Warning", "operation warning"),
    ];

    private static CatalogRow Row(int eventId, string name, string severity, string description)
        => new(eventId, name, $"Vestigium.Helpers.Xml.Events.{name}", severity, description);

    internal readonly record struct CatalogRow(
        int EventId, string Name, string FullName, string Severity, string Description);
}
