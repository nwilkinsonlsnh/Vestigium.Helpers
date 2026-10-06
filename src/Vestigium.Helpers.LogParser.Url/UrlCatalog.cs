using Vestigium.Logging;

namespace Vestigium.Helpers.LogParser.Url;

public static class UrlCatalog
{
    public const string AppId = "LogParser.Url";

    public const string Category = "Helpers";

    public static class Subcategories
    {
        public const string Scan = "Scan";
    }

    public static string LogDirectory => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
        "Vestigium", "Logs", AppId);

    public static void Register(VestigiumLoggerOptions cfg)
    {
        ArgumentNullException.ThrowIfNull(cfg);

        var taxonomy = new VestigiumTaxonomy();
        taxonomy.Register(Category, AppId);
        taxonomy.Register(Category, Subcategories.Scan);
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
        Row(UrlEvents.ScanStart, "ScanStart", Subcategories.Scan, "Information", "scan start"),
        Row(UrlEvents.ScanComplete, "ScanComplete", Subcategories.Scan, "Information", "scan complete"),
        Row(UrlEvents.ScanFailed, "ScanFailed", Subcategories.Scan, "Error", "scan failed"),
        Row(UrlEvents.Oversize, "Oversize", Subcategories.Scan, "Error", "text over 64 MB"),
    ];

    private static CatalogRow Row(int eventId, string name, string subcategory, string severity, string description)
        => new(eventId, name, $"Vestigium.Helpers.LogParser.Url.Events.{name}", subcategory, severity, description);

    internal readonly record struct CatalogRow(
        int EventId,
        string Name,
        string FullName,
        string Subcategory,
        string Severity,
        string Description);
}
