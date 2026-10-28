using Vestigium.Logging;

namespace Vestigium.Helpers.LogParser.Har;

public static class HarCatalog
{
    public const string AppId = "LogParser.Har";

    public const string Category = "Helpers";

    public static class Subcategories
    {
        public const string Parse = "Parse";
    }

    public static string LogDirectory => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
        "Vestigium", "Logs", AppId);

    public static void Register(VestigiumLoggerOptions cfg)
    {
        ArgumentNullException.ThrowIfNull(cfg);

        var taxonomy = new VestigiumTaxonomy();
        taxonomy.Register(Category, AppId);
        taxonomy.Register(Category, Subcategories.Parse);
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
        Row(HarEvents.ParseStart, "ParseStart", Subcategories.Parse, "Information", "parse start"),
        Row(HarEvents.ParseComplete, "ParseComplete", Subcategories.Parse, "Information", "parse complete"),
        Row(HarEvents.ParseFailed, "ParseFailed", Subcategories.Parse, "Error", "parse failed"),
        Row(HarEvents.Oversize, "Oversize", Subcategories.Parse, "Error", "har over 64 MB"),
    ];

    private static CatalogRow Row(int eventId, string name, string subcategory, string severity, string description)
        => new(eventId, name, $"Vestigium.Helpers.LogParser.Har.Events.{name}", subcategory, severity, description);

    internal readonly record struct CatalogRow(
        int EventId,
        string Name,
        string FullName,
        string Subcategory,
        string Severity,
        string Description);
}
