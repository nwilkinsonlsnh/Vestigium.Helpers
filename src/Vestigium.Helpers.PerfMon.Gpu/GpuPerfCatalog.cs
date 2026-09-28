using Vestigium.Logging;

namespace Vestigium.Helpers.PerfMon.Gpu;

/// <summary>
/// Registers the Gpu probe catalog during <see cref="VestigiumLogger.Initialize"/>.
/// This library never initializes the host. Folder follows the host.
/// </summary>
public static class GpuPerfCatalog
{
    public const string AppId = "PerfMon.Gpu";
    public const string Category = "PerfMon";

    public static class Subcategories
    {
        public const string Probe = "Gpu";
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
        Row(GpuPerfEvents.ProbeEnter, "ProbeEnter", Subcategories.Probe, "Debug", "enter probe"),
        Row(GpuPerfEvents.ProbeStarted, "ProbeStarted", Subcategories.Probe, "Information", "probe started"),
        Row(GpuPerfEvents.PathsBuilt, "PathsBuilt", Subcategories.Paths, "Debug", "paths built"),
        Row(GpuPerfEvents.ProbeComplete, "ProbeComplete", Subcategories.Probe, "Information", "probe complete"),
        Row(GpuPerfEvents.ProbeCancelled, "ProbeCancelled", Subcategories.Probe, "Information", "probe cancelled"),
        Row(GpuPerfEvents.ProbeRejected, "ProbeRejected", Subcategories.Probe, "Error", "probe rejected"),
        Row(GpuPerfEvents.ObjectMissing, "ObjectMissing", Subcategories.Paths, "Warning", "GPU object absent"),
        Row(GpuPerfEvents.InstancesCapped, "InstancesCapped", Subcategories.Paths, "Warning", "instance list truncated"),
        Row(GpuPerfEvents.Headless, "Headless", Subcategories.Paths, "Information", "both short-job objects absent"),
        Row(GpuPerfEvents.ProbeFailed, "ProbeFailed", Subcategories.Probe, "Error", "probe aborted after start"),
    ];

    private static CatalogRow Row(int eventId, string name, string subcategory, string severity, string description)
        => new(eventId, name, $"Vestigium.Helpers.PerfMon.Gpu.Events.{name}", subcategory, severity, description);

    internal readonly record struct CatalogRow(
        int EventId,
        string Name,
        string FullName,
        string Subcategory,
        string Severity,
        string Description);
}
