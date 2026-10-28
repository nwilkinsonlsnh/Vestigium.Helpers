namespace Vestigium.Helpers.Kql.AppMgmt;

public static class StartupCatalog
{
    public static IReadOnlyList<KqlField> Fields { get; } =
    [
        F("startup.name", KqlType.String, "name"),
        F("startup.command", KqlType.String, "command"),
        F("startup.source", KqlType.String, "source"),
        F("startup.scope", KqlType.String, "scope"),
        F("startup.isX64", KqlType.Boolean, "isX64"),
        F("startup.isX86", KqlType.Boolean, "isX86"),
        F("startup.package", KqlType.String, "package"),
        F("startup.enabled", KqlType.Boolean, "enabled")
    ];

    private static KqlField F(string canonical, KqlType type, params string[] aliases)
        => KqlField.Create(canonical, type, aliases);
}
