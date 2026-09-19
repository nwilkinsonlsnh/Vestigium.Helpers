using Vestigium.Logging;

namespace Vestigium.Helpers.Network;

public static class NetworkCatalog
{
    public const string AppId = "Network";
    public const string Category = "Helpers";
    public const string Subcategory = "Network";

    public static void Register(VestigiumLoggerOptions cfg)
    {
        ArgumentNullException.ThrowIfNull(cfg);
        var taxonomy = new VestigiumTaxonomy();
        taxonomy.Register(Category, AppId);
        taxonomy.Register(
            Category,
            "Probe", "Inventory", "Adapter", "Connection", "Route", "Neighbor",
            "Netbios", "Icmp", "Dns", "Campaign", "Address", "Bandwidth", "Subnet", "Guard");
        cfg.RegisterTaxonomy(taxonomy);
        foreach (var row in Rows)
            cfg.RegisterEvent(row.Name, row.FullName, Category, Subcategory, row.EventId, row.Severity, row.Description);
    }

    internal static readonly CatalogRow[] Rows =
    [
        Row(NetworkEvents.ProbeEnter, "ProbeEnter", "Debug", "enter Probe"),
        Row(NetworkEvents.ProbeComplete, "ProbeComplete", "Information", "probe complete"),
        Row(NetworkEvents.OperationEnter, "OperationEnter", "Debug", "enter operation"),
        Row(NetworkEvents.OperationComplete, "OperationComplete", "Information", "operation complete"),
        Row(NetworkEvents.OperationFailed, "OperationFailed", "Error", "operation failed"),
        Row(NetworkEvents.OperationWarning, "OperationWarning", "Warning", "operation warning"),
    ];

    private static CatalogRow Row(int eventId, string name, string severity, string description)
        => new(eventId, name, $"Vestigium.Helpers.Network.Events.{name}", severity, description);

    internal readonly record struct CatalogRow(
        int EventId, string Name, string FullName, string Severity, string Description);
}
