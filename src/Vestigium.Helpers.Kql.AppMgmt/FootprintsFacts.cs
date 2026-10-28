namespace Vestigium.Helpers.Kql.AppMgmt;

public sealed class FootprintsFacts
{
    public string? Kind { get; init; }
    public string? Name { get; init; }
    public string? Path { get; init; }
    public string? Scope { get; init; }
    public int? RefCount { get; init; }
    public string? Package { get; init; }
}
