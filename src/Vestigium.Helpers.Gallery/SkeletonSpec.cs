namespace Vestigium.Helpers.Gallery;

public sealed class SkeletonSpec
{
    public required string AppId { get; init; }
    public required string Identity { get; init; }
    public required string Title { get; init; }
    public required string Role { get; init; }
    public required string DocumentId { get; init; }
    public required string Blurb { get; init; }
    public required Func<string> Probe { get; init; }
    public string StatusLabel { get; init; } = "Skeleton";
    public string StatusNote { get; init; } = "Skeleton until its own SRS is accepted. Probe writes Pending then Success through HelperLog.";
}
