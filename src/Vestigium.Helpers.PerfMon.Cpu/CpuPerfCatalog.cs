using Vestigium.Logging;

namespace Vestigium.Helpers.PerfMon.Cpu;

/// <summary>
/// Registers the Cpu probe catalog during <see cref="VestigiumLogger.Initialize"/>.
/// This library never initializes the host. Folder follows the host.
/// </summary>
public static class CpuPerfCatalog
{
    public const string AppId = "PerfMon.Cpu";
    public const string Category = "PerfMon";

    public static class Subcategories
    {
        public const string Probe = "Cpu";
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
        Row(CpuPerfEvents.ProbeEnter, "ProbeEnter", Subcategories.Probe, "Debug", "enter probe"),
        Row(CpuPerfEvents.ProbeStarted, "ProbeStarted", Subcategories.Probe, "Information", "probe started"),
        Row(CpuPerfEvents.PathsBuilt, "PathsBuilt", Subcategories.Paths, "Debug", "paths built"),
        Row(CpuPerfEvents.ProbeComplete, "ProbeComplete", Subcategories.Probe, "Information", "probe complete"),
        Row(CpuPerfEvents.ProbeCancelled, "ProbeCancelled", Subcategories.Probe, "Information", "probe cancelled"),
        Row(CpuPerfEvents.ProbeRejected, "ProbeRejected", Subcategories.Probe, "Error", "probe rejected"),
        Row(CpuPerfEvents.CategoryFallback, "CategoryFallback", Subcategories.Paths, "Information", "Processor Information missing"),
        Row(CpuPerfEvents.ParkingSkipped, "ParkingSkipped", Subcategories.Paths, "Debug", "parking counter absent"),
        Row(CpuPerfEvents.CoresCapped, "CoresCapped", Subcategories.Paths, "Warning", "core list truncated"),
        Row(CpuPerfEvents.ProbeFailed, "ProbeFailed", Subcategories.Probe, "Error", "probe aborted after start"),
    ];

    private static CatalogRow Row(int eventId, string name, string subcategory, string severity, string description)
        => new(eventId, name, $"Vestigium.Helpers.PerfMon.Cpu.Events.{name}", subcategory, severity, description);

    internal readonly record struct CatalogRow(
        int EventId,
        string Name,
        string FullName,
        string Subcategory,
        string Severity,
        string Description);
}
