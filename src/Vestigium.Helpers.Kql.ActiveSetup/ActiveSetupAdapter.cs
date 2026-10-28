namespace Vestigium.Helpers.Kql.ActiveSetup;

public static class ActiveSetupAdapter
{
    public static KqlRow ToRow(KqlSession session, ActiveSetupFacts facts)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(facts);
        return new KqlRow(session)
            .Set("setup.displayname", facts.DisplayName)
            .Set("setup.component", facts.Component)
            .Set("setup.stubpath", facts.StubPath)
            .Set("setup.version", facts.Version)
            .Set("setup.locale", facts.Locale)
            .Set("setup.componentId", facts.ComponentId)
            .Set("setup.installed", facts.Installed)
            .Set("setup.isGlobal", facts.IsGlobal)
            .Set("setup.isX64", facts.IsX64)
            .Set("setup.pending", facts.Pending);
    }
}
