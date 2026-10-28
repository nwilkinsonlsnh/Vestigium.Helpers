namespace Vestigium.Helpers.Kql.AppMgmt;

public static class BloatwareCatalog
{
    public static IReadOnlyList<KqlField> Fields { get; } =
    [
        F("bloat.name", KqlType.String, "name"),
        F("bloat.family", KqlType.String, "family"),
        F("bloat.category", KqlType.String, "category"),
        F("bloat.installed", KqlType.Boolean, "installed"),
        F("bloat.provisioned", KqlType.Boolean, "provisioned"),
        F("bloat.package", KqlType.String, "package")
    ];

    private static KqlField F(string canonical, KqlType type, params string[] aliases)
        => KqlField.Create(canonical, type, aliases);
}
