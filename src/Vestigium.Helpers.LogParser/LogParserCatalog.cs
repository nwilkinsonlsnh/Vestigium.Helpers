using Vestigium.Logging;

namespace Vestigium.Helpers.LogParser;

public static class LogParserCatalog
{
    public const string AppId = "LogParser";

    public const string Category = "Helpers";

    public static class Subcategories
    {
        public const string Bag = "Bag";
    }

    /// <summary>
    /// Host directory for this library. On Windows this is
    /// C:\ProgramData\Vestigium\Logs\LogParser. The library does not create it
    /// and does not call Initialize.
    /// </summary>
    public static string LogDirectory => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
        "Vestigium", "Logs", AppId);

    public static void Register(VestigiumLoggerOptions cfg)
    {
        ArgumentNullException.ThrowIfNull(cfg);

        var taxonomy = new VestigiumTaxonomy();
        taxonomy.Register(Category, AppId);
        taxonomy.Register(Category, Subcategories.Bag);
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
        Row(LogParserEvents.HostRejected, "HostRejected", Subcategories.Bag, "Error", "rejected host"),
        Row(LogParserEvents.ResultRejected, "ResultRejected", Subcategories.Bag, "Error", "rejected read result"),
        Row(LogParserEvents.Thrown, "Thrown", Subcategories.Bag, "Error", "unexpected failure"),
    ];

    private static CatalogRow Row(int eventId, string name, string subcategory, string severity, string description)
        => new(eventId, name, $"Vestigium.Helpers.LogParser.Events.{name}", subcategory, severity, description);

    internal readonly record struct CatalogRow(
        int EventId,
        string Name,
        string FullName,
        string Subcategory,
        string Severity,
        string Description);
}
