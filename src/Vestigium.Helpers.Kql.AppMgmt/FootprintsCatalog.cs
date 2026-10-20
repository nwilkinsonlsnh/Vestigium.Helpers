namespace Vestigium.Helpers.Kql.AppMgmt;

public static class FootprintsCatalog
{
    public static IReadOnlyList<KqlField> Fields { get; } =
    [
        F("footprint.kind", KqlType.String, "kind"),
        F("footprint.name", KqlType.String, "name"),
        F("footprint.path", KqlType.String, "path"),
        F("footprint.scope", KqlType.String, "scope"),
        F("footprint.refcount", KqlType.Integer, "refcount"),
        F("footprint.package", KqlType.String, "package")
    ];

    private static KqlField F(string canonical, KqlType type, params string[] aliases)
        => KqlField.Create(canonical, type, aliases);
}
