namespace Vestigium.Helpers.Kql;

public sealed class KqlField
{
    internal KqlField(
        string canonical,
        KqlType type,
        KqlGroups group,
        IReadOnlyList<KqlPack> packs,
        params string[] aliases)
    {
        Canonical = canonical;
        Type = type;
        Group = group;
        Packs = packs;
        Aliases = aliases;
    }

    public string Canonical { get; }
    public KqlType Type { get; }
    public KqlGroups Group { get; }
    public IReadOnlyList<KqlPack> Packs { get; }
    public IReadOnlyList<string> Aliases { get; }
}
