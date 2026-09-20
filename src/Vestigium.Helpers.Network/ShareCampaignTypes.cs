using Vestigium.Helpers.FileIo;

namespace Vestigium.Helpers.Network;

public enum ShareCampaignMode
{
    Default = 0,
    Advanced = 1
}

public sealed class FileShareTarget
{
    public string Directory { get; set; } = "";
}

public sealed class ShareProbeOptions
{
    public long MaxProbeBytes { get; set; } = 256L * 1024 * 1024;
    public long BucketFloorBytes { get; set; } = 1024 * 1024;
    public bool IncludeReadProbe { get; set; }
}

public sealed class ShareProbeStep
{
    public string Workload { get; init; } = "Sequential";
    public FileIoBucket? Bucket { get; init; }
    public long ProbeBytes { get; init; }
    public int ProbeCount { get; init; } = 1;
    public bool IsMetadata { get; init; }
}

public sealed class ShareProbePlan
{
    public ShareCampaignMode Mode { get; init; }
    public string? SourcePath { get; init; }
    public long PlannedBytes { get; init; }
    public long MaxProbeBytes { get; init; }
    public IReadOnlyList<ShareProbeStep> Probes { get; init; } = [];
}

public sealed class ShareCampaignOptions
{
    public FileShareTarget Target { get; set; } = new();
    public BandwidthAmount? PlannedSize { get; set; }
    public ShareCampaignMode Mode { get; set; } = ShareCampaignMode.Default;
    public double Efficiency { get; set; } = 1.0;
    public long MaxProbeBytes { get; set; } = 256L * 1024 * 1024;
    public long ProbeBytes { get; set; } = 64L * 1024 * 1024;
    public int ProbeCount { get; set; } = 4;
    public bool IncludeReadProbe { get; set; }
    public BandwidthAmount? DeclaredPipeRate { get; set; }
    public FileIoDirectoryAnalysis? SourceAnalysis { get; set; }
    public string? RecipePath { get; set; }
    public string? ResultsPath { get; set; }
}

public sealed record ShareCampaignResult(
    string CampaignId,
    NetworkJobStatus Status,
    ShareCampaignMode Mode,
    string ResultsPath,
    string Disclaimer,
    TimeSpan MeasuredDuration,
    BandwidthAmount? MeasuredRate,
    TimeSpan? DeclaredPipeDuration);
