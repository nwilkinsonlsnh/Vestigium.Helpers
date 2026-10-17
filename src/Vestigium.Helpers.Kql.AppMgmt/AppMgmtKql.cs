namespace Vestigium.Helpers.Kql.AppMgmt;

public static class AppMgmtKql
{
    public static string Identity => "Vestigium.Helpers.Kql.AppMgmt";

    public static KqlSession Packages() => KqlHelper.Create(PackageCatalog.Fields);

    public static KqlSession Features() => KqlHelper.Create(FeatureCatalog.Fields);

    public static KqlSession Bloatware() => KqlHelper.Create(BloatwareCatalog.Fields);
}
