namespace Vestigium.Helpers.Kql;

/// <summary>In-memory row for tests and the gallery until a host binds live data.</summary>
public sealed class KqlFixtureRow : IKqlRow
{
    private readonly KqlRow _inner;

    public KqlFixtureRow(KqlSession session)
        => _inner = new KqlRow(session);

    public KqlFixtureRow Set(string name, object? value)
    {
        _inner.Set(name, value);
        return this;
    }

    public KqlValue Get(string canonical) => _inner.Get(canonical);
}
