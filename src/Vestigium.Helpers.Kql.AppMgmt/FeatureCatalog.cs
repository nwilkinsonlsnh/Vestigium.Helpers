namespace Vestigium.Helpers.Kql.AppMgmt;

public static class FeatureCatalog
{
    public static IReadOnlyList<KqlField> Fields { get; } =
    [
        KqlField.Create("feature.caption", KqlType.String, ["caption"]),
        KqlField.Create("feature.name", KqlType.String, ["name"]),
        KqlField.Create("feature.state", KqlType.Boolean, ["state"]),
        KqlField.Create("feature.IsEnabled", KqlType.Boolean, ["IsEnabled"]),
        KqlField.Create("feature.parent", KqlType.String, ["parent"])
    ];
}
