namespace Vestigium.Helpers.Kql;

public sealed class KqlField
{
    internal KqlField(
        string canonical,
        KqlType type,
        KqlGroups group,
        IReadOnlyList<KqlPack> packs,
        IReadOnlyList<string> aliases,
        bool watchOnly = false)
    {
        Canonical = canonical;
        Type = type;
        Group = group;
        Packs = packs;
        Aliases = aliases;
        WatchOnly = watchOnly;
    }

    public string Canonical { get; }
    public KqlType Type { get; }
    public KqlGroups Group { get; }
    public IReadOnlyList<KqlPack> Packs { get; }
    public IReadOnlyList<string> Aliases { get; }
    public bool WatchOnly { get; }
}
