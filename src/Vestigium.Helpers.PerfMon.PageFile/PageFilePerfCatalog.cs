using Vestigium.Logging;

namespace Vestigium.Helpers.PerfMon.PageFile;

/// <summary>
/// Registers the PageFile probe catalog during <see cref="VestigiumLogger.Initialize"/>.
/// This library never initializes the host. Folder follows the host.
/// </summary>
public static class PageFilePerfCatalog
{
    public const string AppId = "PerfMon.PageFile";
    public const string Category = "PerfMon";

    public static class Subcategories
    {
        public const string Probe = "PageFile";
        public const string Paths = "Paths";
    }

    public static void Register(VestigiumLoggerOptions cfg)
    {
        ArgumentNullException.ThrowIfNull(cfg);

        var taxonomy = new VestigiumTaxonomy();
        taxonomy.Register(Category, AppId);
        taxonomy.Register(Category, Subcategories.Probe, Subcategories.Paths);
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
        Row(PageFilePerfEvents.ProbeEnter, "ProbeEnter", Subcategories.Probe, "Debug", "enter probe"),
        Row(PageFilePerfEvents.ProbeStarted, "ProbeStarted", Subcategories.Probe, "Information", "probe started"),
        Row(PageFilePerfEvents.PathsBuilt, "PathsBuilt", Subcategories.Paths, "Debug", "paths built"),
        Row(PageFilePerfEvents.ProbeComplete, "ProbeComplete", Subcategories.Probe, "Information", "probe complete"),
        Row(PageFilePerfEvents.ProbeCancelled, "ProbeCancelled", Subcategories.Probe, "Information", "probe cancelled"),
        Row(PageFilePerfEvents.ProbeRejected, "ProbeRejected", Subcategories.Probe, "Error", "probe rejected"),
        Row(PageFilePerfEvents.ObjectMissing, "ObjectMissing", Subcategories.Paths, "Warning", "Paging File absent"),
        Row(PageFilePerfEvents.FilesCapped, "FilesCapped", Subcategories.Paths, "Warning", "file list truncated"),
        Row(PageFilePerfEvents.SiblingSkipped, "SiblingSkipped", Subcategories.Paths, "Debug", "Memory not used"),
        Row(PageFilePerfEvents.ProbeFailed, "ProbeFailed", Subcategories.Probe, "Error", "probe aborted after start"),
    ];

    private static CatalogRow Row(int eventId, string name, string subcategory, string severity, string description)
        => new(eventId, name, $"Vestigium.Helpers.PerfMon.PageFile.Events.{name}", subcategory, severity, description);

    internal readonly record struct CatalogRow(
        int EventId,
        string Name,
        string FullName,
        string Subcategory,
        string Severity,
        string Description);
}
