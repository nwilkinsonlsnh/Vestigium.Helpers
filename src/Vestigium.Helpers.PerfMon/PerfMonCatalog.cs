using Vestigium.Logging;

namespace Vestigium.Helpers.PerfMon;

/// <summary>
/// Registers the PerfMon custom catalog during <see cref="VestigiumLogger.Initialize"/>.
/// This library never initializes the host.
/// </summary>
public static class PerfMonCatalog
{
    /// <summary>Suggested APPID when this library is the process. Folder still follows the host.</summary>
    public const string AppId = "PerfMon";

    /// <summary>Catalog category.</summary>
    public const string Category = "PerfMon";

    public static class Subcategories
    {
        public const string Job = "Job";
        public const string Source = "Source";
        public const string Path = "Path";
    }

    /// <summary>
    /// Hosts call this inside <see cref="VestigiumLogger.Initialize"/>.
    /// The library never calls it for them.
    /// </summary>
    public static void Register(VestigiumLoggerOptions cfg)
    {
        ArgumentNullException.ThrowIfNull(cfg);

        var taxonomy = new VestigiumTaxonomy();
        taxonomy.Register(Category, AppId);
        taxonomy.Register(Category, Subcategories.Job, Subcategories.Source, Subcategories.Path);
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
        Row(PerfMonEvents.JobEnter, "JobEnter", Subcategories.Job, "Debug", "enter job"),
        Row(PerfMonEvents.JobStarted, "JobStarted", Subcategories.Job, "Information", "job started"),
        Row(PerfMonEvents.JobTick, "JobTick", Subcategories.Job, "Debug", "job tick"),
        Row(PerfMonEvents.JobComplete, "JobComplete", Subcategories.Job, "Information", "job complete"),
        Row(PerfMonEvents.JobCancelled, "JobCancelled", Subcategories.Job, "Information", "job cancelled"),
        Row(PerfMonEvents.JobRejected, "JobRejected", Subcategories.Job, "Error", "job rejected"),
        Row(PerfMonEvents.SourceUnavailable, "SourceUnavailable", Subcategories.Source, "Warning", "category or instance missing"),
        Row(PerfMonEvents.SourceThrown, "SourceThrown", Subcategories.Source, "Error", "unexpected PDH failure"),
        Row(PerfMonEvents.PathRejected, "PathRejected", Subcategories.Path, "Error", "bad counter path"),
        Row(PerfMonEvents.JobFailed, "JobFailed", Subcategories.Job, "Error", "job aborted after start"),
    ];

    private static CatalogRow Row(int eventId, string name, string subcategory, string severity, string description)
        => new(eventId, name, $"Vestigium.Helpers.PerfMon.Events.{name}", subcategory, severity, description);

    internal readonly record struct CatalogRow(
        int EventId,
        string Name,
        string FullName,
        string Subcategory,
        string Severity,
        string Description);
}
