using Vestigium.Logging;

namespace Vestigium.Helpers.Encryption;

public static class EncryptionCatalog
{
    public const string AppId = "Encryption";
    public const string Category = "Helpers";

    public static class Subcategories
    {
        public const string Encryption = "Encryption";
        public const string Token = "Token";
        public const string Probe = "Probe";
    }

    public static void Register(VestigiumLoggerOptions cfg)
    {
        ArgumentNullException.ThrowIfNull(cfg);
        var taxonomy = new VestigiumTaxonomy();
        taxonomy.Register(Category, AppId);
        taxonomy.Register(Category, Subcategories.Encryption, Subcategories.Token, Subcategories.Probe);
        cfg.RegisterTaxonomy(taxonomy);
        foreach (var row in Rows)
        {
            cfg.RegisterEvent(row.Name, row.FullName, Category, row.Subcategory, row.EventId, row.Severity, row.Description);
        }
    }

    internal static readonly CatalogRow[] Rows =
    [
        Row(EncryptionEvents.ProbeEnter, "ProbeEnter", Subcategories.Probe, "Debug", "enter Probe"),
        Row(EncryptionEvents.ProbeComplete, "ProbeComplete", Subcategories.Probe, "Information", "probe complete"),
        Row(EncryptionEvents.OperationEnter, "OperationEnter", Subcategories.Encryption, "Debug", "enter operation"),
        Row(EncryptionEvents.OperationComplete, "OperationComplete", Subcategories.Encryption, "Information", "operation complete"),
        Row(EncryptionEvents.OperationFailed, "OperationFailed", Subcategories.Encryption, "Error", "operation failed"),
        Row(EncryptionEvents.TokenWarning, "TokenWarning", Subcategories.Token, "Warning", "token warning"),
        Row(EncryptionEvents.FileRejected, "FileRejected", Subcategories.Encryption, "Error", "rejected file"),
    ];

    private static CatalogRow Row(int eventId, string name, string subcategory, string severity, string description)
        => new(eventId, name, $"Vestigium.Helpers.Encryption.Events.{name}", subcategory, severity, description);

    internal readonly record struct CatalogRow(
        int EventId, string Name, string FullName, string Subcategory, string Severity, string Description);
}
