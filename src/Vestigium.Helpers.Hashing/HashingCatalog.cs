using Vestigium.Logging;

namespace Vestigium.Helpers.Hashing;

public static class HashingCatalog
{
    public const string AppId = "Hashing";
    public const string Category = "Helpers";
    public const string Subcategory = "Hashing";

    public static void Register(VestigiumLoggerOptions cfg)
    {
        ArgumentNullException.ThrowIfNull(cfg);
        var taxonomy = new VestigiumTaxonomy();
        taxonomy.Register(Category, AppId);
        taxonomy.Register(Category, Subcategory);
        cfg.RegisterTaxonomy(taxonomy);
        foreach (var row in Rows)
            cfg.RegisterEvent(row.Name, row.FullName, Category, Subcategory, row.EventId, row.Severity, row.Description);
    }

    internal static readonly CatalogRow[] Rows =
    [
        Row(HashingEvents.ProbeEnter, "ProbeEnter", "Debug", "enter Probe"),
        Row(HashingEvents.ProbeComplete, "ProbeComplete", "Information", "probe complete"),
        Row(HashingEvents.OperationEnter, "OperationEnter", "Debug", "enter operation"),
        Row(HashingEvents.OperationComplete, "OperationComplete", "Information", "operation complete"),
        Row(HashingEvents.OperationFailed, "OperationFailed", "Error", "operation failed"),
        Row(HashingEvents.HashComplete, "HashComplete", "Information", "string or bytes hash complete"),
        Row(HashingEvents.HashFileComplete, "HashFileComplete", "Information", "file hash complete"),
        Row(HashingEvents.HmacComplete, "HmacComplete", "Information", "HMAC complete"),
        Row(HashingEvents.KmacComplete, "KmacComplete", "Information", "KMAC complete"),
        Row(HashingEvents.ShakeComplete, "ShakeComplete", "Information", "SHAKE complete"),
        Row(HashingEvents.ChecksumComplete, "ChecksumComplete", "Information", "checksum complete"),
        Row(HashingEvents.PasswordHashed, "PasswordHashed", "Information", "password hashed"),
        Row(HashingEvents.PasswordVerify, "PasswordVerify", "Information", "password verify"),
        Row(HashingEvents.ConvertComplete, "ConvertComplete", "Information", "convert complete"),
        Row(HashingEvents.Rejected, "Rejected", "Error", "input rejected"),
    ];

    private static CatalogRow Row(int eventId, string name, string severity, string description)
        => new(eventId, name, $"Vestigium.Helpers.Hashing.Events.{name}", severity, description);

    internal readonly record struct CatalogRow(
        int EventId, string Name, string FullName, string Severity, string Description);
}
