namespace Vestigium.Helpers.Kql.AppMgmt;

public static class StartupAdapter
{
    public static KqlRow ToRow(KqlSession session, StartupFacts facts)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(facts);

        var row = new KqlRow(session)
            .Set("startup.name", facts.Name)
            .Set("startup.command", facts.Command)
            .Set("startup.source", facts.Source)
            .Set("startup.scope", facts.Scope)
            .Set("startup.package", facts.Package)
            .Set("startup.enabled", facts.Enabled);

        if (facts.IsX64 is { } isX64)
            row.Set("startup.isX64", isX64)
                .Set("startup.isX86", !isX64);

        return row;
    }
}
