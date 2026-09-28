using Vestigium.Logging;

namespace Vestigium.Helpers.PerfMon.Disk;

/// <summary>
/// Registers the Disk probe catalog during <see cref="VestigiumLogger.Initialize"/>.
/// This library never initializes the host. Folder follows the host.
/// </summary>
public static class DiskPerfCatalog
{
    public const string AppId = "PerfMon.Disk";
    public const string Category = "PerfMon";

    public static class Subcategories
    {
        public const string Probe = "Disk";
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
        Row(DiskPerfEvents.ProbeEnter, "ProbeEnter", Subcategories.Probe, "Debug", "enter probe"),
        Row(DiskPerfEvents.ProbeStarted, "ProbeStarted", Subcategories.Probe, "Information", "probe started"),
        Row(DiskPerfEvents.PathsBuilt, "PathsBuilt", Subcategories.Paths, "Debug", "paths built"),
        Row(DiskPerfEvents.ProbeComplete, "ProbeComplete", Subcategories.Probe, "Information", "probe complete"),
        Row(DiskPerfEvents.ProbeCancelled, "ProbeCancelled", Subcategories.Probe, "Information", "probe cancelled"),
        Row(DiskPerfEvents.ProbeRejected, "ProbeRejected", Subcategories.Probe, "Error", "probe rejected"),
        Row(DiskPerfEvents.ObjectMissing, "ObjectMissing", Subcategories.Paths, "Warning", "PhysicalDisk absent"),
        Row(DiskPerfEvents.DisksCapped, "DisksCapped", Subcategories.Paths, "Warning", "disk list truncated"),
        Row(DiskPerfEvents.CounterOmitted, "CounterOmitted", Subcategories.Paths, "Debug", "optional counter absent"),
        Row(DiskPerfEvents.ProbeFailed, "ProbeFailed", Subcategories.Probe, "Error", "probe aborted after start"),
    ];

    private static CatalogRow Row(int eventId, string name, string subcategory, string severity, string description)
        => new(eventId, name, $"Vestigium.Helpers.PerfMon.Disk.Events.{name}", subcategory, severity, description);

    internal readonly record struct CatalogRow(
        int EventId,
        string Name,
        string FullName,
        string Subcategory,
        string Severity,
        string Description);
}
