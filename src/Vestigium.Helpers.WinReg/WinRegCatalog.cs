using Vestigium.Logging;

namespace Vestigium.Helpers.WinReg;

public static class WinRegCatalog
{
    public const string AppId = "WinReg";
    public const string Category = "Helpers";
    public const string Subcategory = "WinReg";

    public static void Register(VestigiumLoggerOptions cfg)
    {
        ArgumentNullException.ThrowIfNull(cfg);
        var taxonomy = new VestigiumTaxonomy();
        taxonomy.Register(Category, AppId);
        taxonomy.Register(
            Category,
            "Probe", "Inventory", "Query", "Compare", "Journal", "Export", "Import",
            "Write", "Acl", "Guard", "Safety", "Session");
        cfg.RegisterTaxonomy(taxonomy);
        foreach (var row in Rows)
            cfg.RegisterEvent(row.Name, row.FullName, Category, Subcategory, row.EventId, row.Severity, row.Description);
    }

    internal static readonly CatalogRow[] Rows =
    [
        Row(WinRegEvents.ProbeEnter, "ProbeEnter", "Information", "enter Probe"),
        Row(WinRegEvents.ProbeComplete, "ProbeComplete", "Information", "probe complete"),
        Row(WinRegEvents.OperationEnter, "OperationEnter", "Debug", "enter operation"),
        Row(WinRegEvents.OperationComplete, "OperationComplete", "Information", "operation complete"),
        Row(WinRegEvents.OperationFailed, "OperationFailed", "Error", "operation failed"),
        Row(WinRegEvents.OperationWarning, "OperationWarning", "Warning", "operation warning"),
    ];

    private static CatalogRow Row(int eventId, string name, string severity, string description)
        => new(eventId, name, $"Vestigium.Helpers.WinReg.Events.{name}", severity, description);

    internal readonly record struct CatalogRow(
        int EventId, string Name, string FullName, string Severity, string Description);
}
