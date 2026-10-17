namespace Vestigium.Helpers.Kql.AppMgmt;

public sealed class BloatwareFacts
{
    public string? Name { get; init; }
    public string? Family { get; init; }
    public string? Category { get; init; }
    public bool Installed { get; init; }
    public bool Provisioned { get; init; }
    public string? Package { get; init; }
}
