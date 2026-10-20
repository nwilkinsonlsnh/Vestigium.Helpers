namespace Vestigium.Helpers.Kql.AppMgmt;

public static class FootprintsAdapter
{
    public static KqlRow ToRow(KqlSession session, FootprintsFacts facts)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(facts);

        var row = new KqlRow(session)
            .Set("footprint.kind", facts.Kind)
            .Set("footprint.name", facts.Name)
            .Set("footprint.path", facts.Path)
            .Set("footprint.scope", facts.Scope)
            .Set("footprint.package", facts.Package);

        if (facts.RefCount is { } count)
            row.Set("footprint.refcount", count);

        return row;
    }
}
