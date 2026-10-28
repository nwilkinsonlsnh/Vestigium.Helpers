namespace Vestigium.Helpers.Kql.AppMgmt;

public sealed class FeatureFacts
{
    public string? Caption { get; init; }
    public string? Name { get; init; }
    public string? State { get; init; }
    public bool IsEnabled { get; init; }
    public string? Parent { get; init; }
}
