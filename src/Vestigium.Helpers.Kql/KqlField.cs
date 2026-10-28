namespace Vestigium.Helpers.Kql;

public sealed class KqlField
{
    internal KqlField(
        string canonical,
        KqlType type,
        KqlGroups group,
        IReadOnlyList<KqlPack> packs,
        IReadOnlyList<string> aliases,
        bool watchOnly = false,
        IReadOnlyList<string>? closed = null,
        KqlCompareAs compareAs = KqlCompareAs.Stored,
        int? minimum = null,
        int? maximum = null)
    {
        Canonical = canonical;
        Type = type;
        Group = group;
        Packs = packs;
        Aliases = aliases;
        WatchOnly = watchOnly;
        Closed = closed ?? [];
        CompareAs = compareAs;
        Minimum = minimum;
        Maximum = maximum;
    }

    public static KqlField Create(string canonical, Type type, IReadOnlyList<string>? aliases = null, IReadOnlyList<string>? closed = null)
        => Create(canonical, KqlTypes.From(type), aliases, closed);

    public static KqlField Create(
        string canonical,
        KqlType type,
        IReadOnlyList<string>? aliases = null,
        IReadOnlyList<string>? closed = null,
        KqlCompareAs compareAs = KqlCompareAs.Stored,
        int? minimum = null,
        int? maximum = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(canonical);
        return new KqlField(
            canonical.Trim(),
            type,
            KqlGroups.None,
            [],
            aliases ?? [],
            false,
            closed,
            compareAs,
            minimum,
            maximum);
    }

    public string Canonical { get; }
    public KqlType Type { get; }
    public KqlGroups Group { get; }
    public IReadOnlyList<KqlPack> Packs { get; }
    public IReadOnlyList<string> Aliases { get; }
    public bool WatchOnly { get; }
    public IReadOnlyList<string> Closed { get; }
    public KqlCompareAs CompareAs { get; }
    public int? Minimum { get; }
    public int? Maximum { get; }

    public bool IsClosed(string token)
    {
        if (string.IsNullOrWhiteSpace(token) || Closed.Count == 0)
            return false;
        foreach (var value in Closed)
        {
            if (value.Equals(token, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }
}
