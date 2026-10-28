namespace Vestigium.Helpers.Kql;

public sealed class KqlRow(KqlSession session) : IKqlRow
{
    private readonly KqlSession _session = session ?? throw new ArgumentNullException(nameof(session));
    private readonly Dictionary<string, KqlValue> _values = new(StringComparer.OrdinalIgnoreCase);

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
