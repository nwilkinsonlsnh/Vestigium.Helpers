namespace Vestigium.Helpers.Kql.ActiveSetup;

public sealed class ActiveSetupFacts
{
    public string? DisplayName { get; init; }
    public string? Component { get; init; }
    public string? StubPath { get; init; }
    public string? Version { get; init; }
    public string? Locale { get; init; }
    public string? ComponentId { get; init; }
    public bool Installed { get; init; }
    public bool IsGlobal { get; init; }
    public bool IsX64 { get; init; }
    public bool Pending { get; init; }
}
