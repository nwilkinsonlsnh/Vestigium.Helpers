namespace Vestigium.Helpers.Kql.AppMgmt;

public static class PackageCatalog
{
    public static IReadOnlyList<KqlField> Fields { get; } =
    [
        F("app.displayname", KqlType.String, "displayname"),
        F("app.publisher", KqlType.String, "publisher"),
        F("app.version", KqlType.String, "version"),
        F("app.MajorVersion", KqlType.Integer, "MajorVersion"),
        F("app.MinorVersion", KqlType.Integer, "MinorVersion"),
        F("app.InstallDate", KqlType.DateTime, "InstallDate"),
        F("app.size.kb", KqlType.Number),
        F("app.size.mb", KqlType.Number),
        F("app.size.gb", KqlType.Number),
        F("app.IsX64", KqlType.Boolean, "IsX64"),
        F("app.IsX86", KqlType.Boolean, "IsX86"),
        F("app.IsGlobal", KqlType.Boolean, "IsGlobal"),
        F("app.IsSystemComponent", KqlType.Boolean, "IsSystemComponent"),
        F("app.WindowsInstaller", KqlType.Boolean, "WindowsInstaller"),
        F("app.NoModify", KqlType.Boolean, "NoModify"),
        F("app.NoRemove", KqlType.Boolean, "NoRemove"),
        F("app.NoRepair", KqlType.Boolean, "NoRepair"),
        F("app.HasIcon", KqlType.Boolean, "HasIcon"),
        F("app.HasModify", KqlType.Boolean, "HasModify"),
        F("app.HasRepair", KqlType.Boolean, "HasRepair"),
        F("app.IsOrphan", KqlType.Boolean, "IsOrphan"),
        F("app.BaselineStatus", KqlType.String, "BaselineStatus"),
        F("app.PendingReboot", KqlType.Boolean, "PendingReboot")
    ];

    private static KqlField F(string canonical, KqlType type, params string[] aliases)
        => KqlField.Create(canonical, type, aliases);
}
