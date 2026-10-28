namespace Vestigium.Helpers.Kql.AppMgmt;

public static class BloatwareAdapter
{
    public static KqlRow ToRow(KqlSession session, BloatwareFacts facts)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(facts);
        return new KqlRow(session)
            .Set("bloat.name", facts.Name)
            .Set("bloat.family", facts.Family)
            .Set("bloat.category", facts.Category)
            .Set("bloat.installed", facts.Installed)
            .Set("bloat.provisioned", facts.Provisioned)
            .Set("bloat.package", facts.Package);
    }
}
