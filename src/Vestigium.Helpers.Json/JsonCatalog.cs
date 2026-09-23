using Vestigium.Logging;

namespace Vestigium.Helpers.Json;

public static class JsonCatalog
{
    public const string AppId = "Json";
    public const string Category = "Helpers";
    public const string Subcategory = "Json";

    public static void Register(VestigiumLoggerOptions cfg)
    {
        ArgumentNullException.ThrowIfNull(cfg);
        var taxonomy = new VestigiumTaxonomy();
        taxonomy.Register(Category, AppId);
        taxonomy.Register(
            Category,
            "Probe", "Document", "Session", "Jsonl", "Save", "Query", "Snapshot", "Diff", "Commit", "Guard");
        cfg.RegisterTaxonomy(taxonomy);
        foreach (var row in Rows)
            cfg.RegisterEvent(row.Name, row.FullName, Category, row.Subcategory, row.EventId, row.Severity, row.Description);
    }

    internal static readonly CatalogRow[] Rows =
    [
        Row(JsonEvents.ProbeEnter, "ProbeEnter", "Debug", HelperLog.Subcategories.Probe, "enter Probe"),
        Row(JsonEvents.ProbeComplete, "ProbeComplete", "Information", HelperLog.Subcategories.Probe, "probe complete"),
        Row(JsonEvents.SessionEnter, "SessionEnter", "Debug", HelperLog.Subcategories.Session, "enter session"),
        Row(JsonEvents.SessionComplete, "SessionComplete", "Information", HelperLog.Subcategories.Session, "session complete"),
        Row(JsonEvents.SessionFailed, "SessionFailed", "Error", HelperLog.Subcategories.Session, "session failed"),
        Row(JsonEvents.DocumentEnter, "DocumentEnter", "Debug", HelperLog.Subcategories.Document, "enter document"),
        Row(JsonEvents.DocumentComplete, "DocumentComplete", "Information", HelperLog.Subcategories.Document, "document complete"),
        Row(JsonEvents.DocumentFailed, "DocumentFailed", "Error", HelperLog.Subcategories.Document, "document failed"),
        Row(JsonEvents.QueryEnter, "QueryEnter", "Debug", HelperLog.Subcategories.Query, "enter query"),
        Row(JsonEvents.QueryComplete, "QueryComplete", "Information", HelperLog.Subcategories.Query, "query complete"),
        Row(JsonEvents.QueryFailed, "QueryFailed", "Error", HelperLog.Subcategories.Query, "query failed"),
        Row(JsonEvents.SnapshotEnter, "SnapshotEnter", "Debug", HelperLog.Subcategories.Snapshot, "enter snapshot"),
        Row(JsonEvents.SnapshotComplete, "SnapshotComplete", "Information", HelperLog.Subcategories.Snapshot, "snapshot complete"),
        Row(JsonEvents.DiffEnter, "DiffEnter", "Debug", HelperLog.Subcategories.Diff, "enter diff"),
        Row(JsonEvents.DiffComplete, "DiffComplete", "Information", HelperLog.Subcategories.Diff, "diff complete"),
        Row(JsonEvents.CommitEnter, "CommitEnter", "Debug", HelperLog.Subcategories.Commit, "enter commit"),
        Row(JsonEvents.CommitComplete, "CommitComplete", "Information", HelperLog.Subcategories.Commit, "commit complete"),
        Row(JsonEvents.SaveEnter, "SaveEnter", "Debug", HelperLog.Subcategories.Save, "enter save"),
        Row(JsonEvents.SaveComplete, "SaveComplete", "Information", HelperLog.Subcategories.Save, "save complete"),
        Row(JsonEvents.SaveWarning, "SaveWarning", "Warning", HelperLog.Subcategories.Save, "save warning"),
        Row(JsonEvents.SaveFailed, "SaveFailed", "Error", HelperLog.Subcategories.Save, "save failed"),
        Row(JsonEvents.JsonlEnter, "JsonlEnter", "Debug", HelperLog.Subcategories.Jsonl, "enter jsonl"),
        Row(JsonEvents.JsonlComplete, "JsonlComplete", "Information", HelperLog.Subcategories.Jsonl, "jsonl complete"),
        Row(JsonEvents.JsonlFailed, "JsonlFailed", "Error", HelperLog.Subcategories.Jsonl, "jsonl failed"),
        Row(JsonEvents.GuardFailed, "GuardFailed", "Error", HelperLog.Subcategories.Guard, "guard failed"),
        Row(JsonEvents.OperationWarning, "OperationWarning", "Warning", HelperLog.Subcategories.Guard, "operation warning"),
    ];

    private static CatalogRow Row(int eventId, string name, string severity, string subcategory, string description)
        => new(eventId, name, $"Vestigium.Helpers.Json.Events.{name}", severity, subcategory, description);

    internal readonly record struct CatalogRow(
        int EventId, string Name, string FullName, string Severity, string Subcategory, string Description);
}
