using Vestigium.Logging;

namespace Vestigium.Helpers.Json;

public static class JsonCatalog
{
    public const string AppId = "Json";
    public const string Category = "Helpers";
    public const string Subcategory = "Json";

    public static void Register(VestigiumLoggerOptions cfg)
    {
        ArgumentNullException.ThrowIfNull(cfg);
        var taxonomy = new VestigiumTaxonomy();
        taxonomy.Register(Category, AppId);
        taxonomy.Register(
            Category,
            "Probe", "Document", "Session", "Jsonl", "Save", "Query", "Snapshot", "Diff", "Commit", "Guard");
        cfg.RegisterTaxonomy(taxonomy);
        foreach (var row in Rows)
            cfg.RegisterEvent(row.Name, row.FullName, Category, Subcategory, row.EventId, row.Severity, row.Description);
    }

    internal static readonly CatalogRow[] Rows =
    [
        Row(JsonEvents.ProbeEnter, "ProbeEnter", "Debug", "enter Probe"),
        Row(JsonEvents.ProbeComplete, "ProbeComplete", "Information", "probe complete"),
        Row(JsonEvents.OperationEnter, "OperationEnter", "Debug", "enter operation"),
        Row(JsonEvents.OperationComplete, "OperationComplete", "Information", "operation complete"),
        Row(JsonEvents.OperationFailed, "OperationFailed", "Error", "operation failed"),
        Row(JsonEvents.OperationWarning, "OperationWarning", "Warning", "operation warning"),
    ];

    private static CatalogRow Row(int eventId, string name, string severity, string description)
        => new(eventId, name, $"Vestigium.Helpers.Json.Events.{name}", severity, description);

    internal readonly record struct CatalogRow(
        int EventId, string Name, string FullName, string Severity, string Description);
}
