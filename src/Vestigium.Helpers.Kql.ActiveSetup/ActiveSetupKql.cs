namespace Vestigium.Helpers.Kql.ActiveSetup;

public static class ActiveSetupKql
{
    public static string Identity => "Vestigium.Helpers.Kql.ActiveSetup";

    public static KqlSession Components() => KqlHelper.Create(ActiveSetupCatalog.Fields);
}
