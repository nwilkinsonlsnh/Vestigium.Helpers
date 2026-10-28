using Vestigium.Logging;

namespace Vestigium.Helpers.Kql;

public static class KqlLoggingCatalog
{
    public const string AppId = "Kql";
    public const string Category = "Helpers";
    public const string Subcategory = "Kql";

    public static void Register(VestigiumLoggerOptions cfg)
    {
        ArgumentNullException.ThrowIfNull(cfg);
        var taxonomy = new VestigiumTaxonomy();
        taxonomy.Register(Category, AppId);
        taxonomy.Register(Category, "Probe", "Session", "Query");
        cfg.RegisterTaxonomy(taxonomy);
        foreach (var row in Rows)
            cfg.RegisterEvent(row.Name, row.FullName, Category, Subcategory, row.EventId, row.Severity, row.Description);
    }

    internal static readonly CatalogRow[] Rows =
    [
        Row(KqlEvents.ProbeEnter, "ProbeEnter", "Information", "enter Probe"),
        Row(KqlEvents.ProbeComplete, "ProbeComplete", "Information", "probe complete"),
        Row(KqlEvents.SessionCreated, "SessionCreated", "Information", "session created"),
        Row(KqlEvents.QueryWarning, "QueryWarning", "Warning", "query warning"),
        Row(KqlEvents.QueryFailed, "QueryFailed", "Warning", "query failed"),
    ];

    private static CatalogRow Row(int eventId, string name, string severity, string description)
        => new(eventId, name, $"Vestigium.Helpers.Kql.Events.{name}", severity, description);

    internal readonly record struct CatalogRow(
        int EventId, string Name, string FullName, string Severity, string Description);
}
