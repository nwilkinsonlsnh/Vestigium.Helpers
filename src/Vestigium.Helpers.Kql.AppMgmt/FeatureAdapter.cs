namespace Vestigium.Helpers.Kql.AppMgmt;

public static class FeatureAdapter
{
    public static KqlRow ToRow(KqlSession session, FeatureFacts facts)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(facts);

        return new KqlRow(session)
            .Set("feature.caption", facts.Caption)
            .Set("feature.name", facts.Name)
            .Set("feature.state", facts.IsEnabled)
            .Set("feature.IsEnabled", facts.IsEnabled)
            .Set("feature.parent", facts.Parent);
    }
}
