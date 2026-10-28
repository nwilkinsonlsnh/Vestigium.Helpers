using Vestigium.Logging;

namespace Vestigium.Helpers.Services;

public static class ServicesCatalog
{
    public const string AppId = "Services";
    public const string Category = "Helpers";
    public const string Subcategory = "Services";

    public static void Register(VestigiumLoggerOptions cfg)
    {
        ArgumentNullException.ThrowIfNull(cfg);
        var taxonomy = new VestigiumTaxonomy();
        taxonomy.Register(Category, AppId);
        taxonomy.Register(
            Category,
            "Probe", "Inventory", "Query", "Campaign", "Watch", "Start", "Kill",
            "System", "Guard", "Safety", "Process");
        cfg.RegisterTaxonomy(taxonomy);
        foreach (var row in Rows)
            cfg.RegisterEvent(row.Name, row.FullName, Category, Subcategory, row.EventId, row.Severity, row.Description);
    }

    internal static readonly CatalogRow[] Rows =
    [
        Row(ServicesEvents.ProbeEnter, "ProbeEnter", "Information", "enter Probe"),
        Row(ServicesEvents.ProbeComplete, "ProbeComplete", "Information", "probe complete"),
        Row(ServicesEvents.OperationEnter, "OperationEnter", "Debug", "enter operation"),
        Row(ServicesEvents.OperationComplete, "OperationComplete", "Information", "operation complete"),
        Row(ServicesEvents.OperationFailed, "OperationFailed", "Error", "operation failed"),
        Row(ServicesEvents.OperationWarning, "OperationWarning", "Warning", "operation warning"),
    ];

    private static CatalogRow Row(int eventId, string name, string severity, string description)
        => new(eventId, name, $"Vestigium.Helpers.Services.Events.{name}", severity, description);

    internal readonly record struct CatalogRow(
        int EventId, string Name, string FullName, string Severity, string Description);
}
