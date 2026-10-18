namespace Vestigium.Helpers.Kql.ActiveSetup;

public static class ActiveSetupCatalog
{
    public static IReadOnlyList<KqlField> Fields { get; } =
    [
        F("setup.displayname", KqlType.String, "displayname", "name"),
        F("setup.component", KqlType.String, "component", "id"),
        F("setup.stubpath", KqlType.String, "stubpath"),
        F("setup.version", KqlType.String, "version"),
        F("setup.locale", KqlType.String, "locale"),
        F("setup.componentId", KqlType.String, "componentId"),
        F("setup.installed", KqlType.Boolean, "installed"),
        F("setup.isGlobal", KqlType.Boolean, "isGlobal"),
        F("setup.isX64", KqlType.Boolean, "isX64"),
        F("setup.pending", KqlType.Boolean, "pending")
    ];

    private static KqlField F(string canonical, KqlType type, params string[] aliases)
        => KqlField.Create(canonical, type, aliases);
}
