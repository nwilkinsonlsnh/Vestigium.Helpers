using Vestigium.Logging;

namespace Vestigium.Helpers.FileIo;

public static class FileIoCatalog
{
    public const string AppId = "FileIo";
    public const string Category = "Helpers";

    public static void Register(VestigiumLoggerOptions cfg)
    {
        ArgumentNullException.ThrowIfNull(cfg);
        var taxonomy = new VestigiumTaxonomy();
        taxonomy.Register(Category, AppId);
        taxonomy.Register(
            Category,
            FileIoLog.Subcategories.Job,
            FileIoLog.Subcategories.Recon,
            FileIoLog.Subcategories.Copy,
            FileIoLog.Subcategories.Move,
            FileIoLog.Subcategories.Delete,
            FileIoLog.Subcategories.Mirror,
            FileIoLog.Subcategories.Analyze,
            FileIoLog.Subcategories.Compare,
            FileIoLog.Subcategories.Probe,
            FileIoLog.Subcategories.SecureDelete,
            FileIoLog.Subcategories.Prune,
            FileIoLog.Subcategories.Index,
            FileIoLog.Subcategories.Stats,
            FileIoLog.Subcategories.Progress);
        cfg.RegisterTaxonomy(taxonomy);
        foreach (var row in Rows)
            cfg.RegisterEvent(row.Name, row.FullName, Category, row.Subcategory, row.EventId, row.Severity, row.Description);
    }

    internal static readonly CatalogRow[] Rows =
    [
        Row(FileIoEvents.ProbeEnter, "ProbeEnter", FileIoLog.Subcategories.Probe, "Debug", "enter Probe"),
        Row(FileIoEvents.ProbeComplete, "ProbeComplete", FileIoLog.Subcategories.Probe, "Information", "probe complete"),
        Row(FileIoEvents.OperationEnter, "OperationEnter", FileIoLog.Subcategories.Job, "Debug", "enter operation"),
        Row(FileIoEvents.OperationComplete, "OperationComplete", FileIoLog.Subcategories.Job, "Information", "operation complete"),
        Row(FileIoEvents.OperationFailed, "OperationFailed", FileIoLog.Subcategories.Job, "Error", "operation failed"),
        Row(FileIoEvents.OperationWarning, "OperationWarning", FileIoLog.Subcategories.Job, "Warning", "operation warning"),
        Row(FileIoEvents.PathRejected, "PathRejected", FileIoLog.Subcategories.Job, "Error", "rejected path"),
        Row(FileIoEvents.JobStart, "JobStart", FileIoLog.Subcategories.Job, "Information", "job start"),
        Row(FileIoEvents.JobComplete, "JobComplete", FileIoLog.Subcategories.Job, "Information", "job complete"),
        Row(FileIoEvents.JobCancelled, "JobCancelled", FileIoLog.Subcategories.Job, "Warning", "job cancelled"),
        Row(FileIoEvents.ReconStart, "ReconStart", FileIoLog.Subcategories.Recon, "Debug", "recon start"),
        Row(FileIoEvents.ReconComplete, "ReconComplete", FileIoLog.Subcategories.Recon, "Information", "recon complete"),
        Row(FileIoEvents.ConsumersReleased, "ConsumersReleased", FileIoLog.Subcategories.Job, "Information", "consumers released"),
        Row(FileIoEvents.Decision, "Decision", FileIoLog.Subcategories.Job, "Information", "decision"),
        Row(FileIoEvents.NameCap, "NameCap", FileIoLog.Subcategories.Job, "Error", "name cap"),
        Row(FileIoEvents.ItemInUse, "ItemInUse", FileIoLog.Subcategories.Job, "Error", "in use"),
        Row(FileIoEvents.ItemUnauthorized, "ItemUnauthorized", FileIoLog.Subcategories.Job, "Error", "unauthorized"),
        Row(FileIoEvents.IndexBuilt, "IndexBuilt", FileIoLog.Subcategories.Index, "Information", "index built"),
        Row(FileIoEvents.IndexHit, "IndexHit", FileIoLog.Subcategories.Index, "Information", "skip duplicate"),
        Row(FileIoEvents.ProgressSnapshot, "ProgressSnapshot", FileIoLog.Subcategories.Progress, "Information", "progress snapshot"),
        Row(FileIoEvents.StatsFinalize, "StatsFinalize", FileIoLog.Subcategories.Stats, "Information", "stats"),
        Row(FileIoEvents.JobPaused, "JobPaused", FileIoLog.Subcategories.Job, "Warning", "pause"),
        Row(FileIoEvents.JobResumed, "JobResumed", FileIoLog.Subcategories.Job, "Information", "resume"),
    ];

    private static CatalogRow Row(int eventId, string name, string subcategory, string severity, string description)
        => new(eventId, name, $"Vestigium.Helpers.FileIo.Events.{name}", subcategory, severity, description);

    internal readonly record struct CatalogRow(
        int EventId, string Name, string FullName, string Subcategory, string Severity, string Description);
}
