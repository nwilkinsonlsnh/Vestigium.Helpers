namespace Vestigium.Helpers.Kql;

public sealed class KqlOptions
{
    public IReadOnlyList<KqlPack> Packs { get; init; } = [KqlPack.Process];
    public KqlGroups Groups { get; init; }
}
