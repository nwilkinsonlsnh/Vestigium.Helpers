using Vestigium.Logging;

namespace Vestigium.Helpers.Processes;

public static class ProcessesCatalog
{
    public const string AppId = "Processes";
    public const string Category = "Helpers";
    public const string Subcategory = "Processes";

    public static void Register(VestigiumLoggerOptions cfg)
    {
        ArgumentNullException.ThrowIfNull(cfg);
        var taxonomy = new VestigiumTaxonomy();
        taxonomy.Register(Category, AppId);
        taxonomy.Register(
            Category,
            "Probe", "Inventory", "Query", "Campaign", "Process", "Thread",
            "Watch", "Start", "Kill", "System", "Guard", "Safety");
        cfg.RegisterTaxonomy(taxonomy);
        foreach (var row in Rows)
            cfg.RegisterEvent(row.Name, row.FullName, Category, Subcategory, row.EventId, row.Severity, row.Description);
    }

    internal static readonly CatalogRow[] Rows =
    [
        Row(ProcessesEvents.ProbeEnter, "ProbeEnter", "Information", "enter Probe"),
        Row(ProcessesEvents.ProbeComplete, "ProbeComplete", "Information", "probe complete"),
        Row(ProcessesEvents.OperationEnter, "OperationEnter", "Debug", "enter operation"),
        Row(ProcessesEvents.OperationComplete, "OperationComplete", "Information", "operation complete"),
        Row(ProcessesEvents.OperationFailed, "OperationFailed", "Error", "operation failed"),
        Row(ProcessesEvents.OperationWarning, "OperationWarning", "Warning", "operation warning"),
    ];

    private static CatalogRow Row(int eventId, string name, string severity, string description)
        => new(eventId, name, $"Vestigium.Helpers.Processes.Events.{name}", severity, description);

    internal readonly record struct CatalogRow(
        int EventId, string Name, string FullName, string Severity, string Description);
}
