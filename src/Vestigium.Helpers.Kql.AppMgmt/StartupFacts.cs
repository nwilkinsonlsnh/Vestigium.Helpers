namespace Vestigium.Helpers.Kql.AppMgmt;

public sealed class StartupFacts
{
    public string? Name { get; init; }
    public string? Command { get; init; }
    public string? Source { get; init; }
    public string? Scope { get; init; }
    public bool? IsX64 { get; init; }
    public string? Package { get; init; }
    public bool Enabled { get; init; } = true;
}
