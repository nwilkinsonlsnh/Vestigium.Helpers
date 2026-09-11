namespace Vestigium.Helpers.Kql;

public sealed class KqlRow : IKqlRow
{
    private readonly KqlSession _session;
    private readonly Dictionary<string, KqlValue> _values = new(StringComparer.OrdinalIgnoreCase);

    public KqlRow(KqlSession session)
        => _session = session ?? throw new ArgumentNullException(nameof(session));

    public KqlRow Set(string name, object? value)
    {
        if (!_session.TryGetField(name, out var field))
            throw new ArgumentException($"unknown field '{name}'", nameof(name));
        _values[field.Canonical] = KqlValue.From(value);
        return this;
    }

    public KqlValue Get(string canonical)
        => _values.TryGetValue(canonical, out var value) ? value : KqlValue.Unknown;
}
