using Vestigium.Logging;

namespace Vestigium.Helpers.PerfMon.Memory;

/// <summary>
/// Registers the Memory probe catalog during <see cref="VestigiumLogger.Initialize"/>.
/// This library never initializes the host. Folder follows the host.
/// </summary>
public static class MemoryPerfCatalog
{
    public const string AppId = "PerfMon.Memory";
    public const string Category = "PerfMon";

    public static class Subcategories
    {
        public const string Probe = "Memory";
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
        Row(MemoryPerfEvents.ProbeEnter, "ProbeEnter", Subcategories.Probe, "Debug", "enter probe"),
        Row(MemoryPerfEvents.ProbeStarted, "ProbeStarted", Subcategories.Probe, "Information", "probe started"),
        Row(MemoryPerfEvents.PathsBuilt, "PathsBuilt", Subcategories.Paths, "Debug", "paths built"),
        Row(MemoryPerfEvents.ProbeComplete, "ProbeComplete", Subcategories.Probe, "Information", "probe complete"),
        Row(MemoryPerfEvents.ProbeCancelled, "ProbeCancelled", Subcategories.Probe, "Information", "probe cancelled"),
        Row(MemoryPerfEvents.ProbeRejected, "ProbeRejected", Subcategories.Probe, "Error", "probe rejected"),
        Row(MemoryPerfEvents.ObjectMissing, "ObjectMissing", Subcategories.Paths, "Warning", "Memory object absent"),
        Row(MemoryPerfEvents.SiblingSkipped, "SiblingSkipped", Subcategories.Paths, "Debug", "PageFile not used"),
        Row(MemoryPerfEvents.OptionalAbsent, "OptionalAbsent", Subcategories.Paths, "Debug", "Hyper-V / ReadyBoost / NUMA absent"),
        Row(MemoryPerfEvents.ProbeFailed, "ProbeFailed", Subcategories.Probe, "Error", "probe aborted after start"),
    ];

    private static CatalogRow Row(int eventId, string name, string subcategory, string severity, string description)
        => new(eventId, name, $"Vestigium.Helpers.PerfMon.Memory.Events.{name}", subcategory, severity, description);

    internal readonly record struct CatalogRow(
        int EventId,
        string Name,
        string FullName,
        string Subcategory,
        string Severity,
        string Description);
}
