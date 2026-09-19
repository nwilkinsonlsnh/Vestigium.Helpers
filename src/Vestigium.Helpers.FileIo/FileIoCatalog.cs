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
    ];

    private static CatalogRow Row(int eventId, string name, string subcategory, string severity, string description)
        => new(eventId, name, $"Vestigium.Helpers.FileIo.Events.{name}", subcategory, severity, description);

    internal readonly record struct CatalogRow(
        int EventId, string Name, string FullName, string Subcategory, string Severity, string Description);
}
