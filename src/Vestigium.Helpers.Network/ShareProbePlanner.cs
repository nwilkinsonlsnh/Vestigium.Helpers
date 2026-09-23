using Vestigium.Helpers.FileIo;

namespace Vestigium.Helpers.Network;

internal static class ShareProbePlanner
{
    public const long MinProbeBytes = 4L * 1024;
    public const long MaxStepBytes = 64L * 1024 * 1024;
    public const int MetadataFileCount = 256;
    public const long MetadataFileBytes = 4L * 1024;

    public static ShareProbePlan Plan(FileIoDirectoryAnalysis source, ShareProbeOptions? options)
    {
        ArgumentNullException.ThrowIfNull(source);
        var o = options ?? new ShareProbeOptions();
        if (o.MaxProbeBytes < 1024 * 1024)
            throw new ArgumentOutOfRangeException(nameof(options), "MaxProbeBytes must be at least 1 MiB.");

        var planned = o.PlannedBytesOverride ?? source.TotalBytes;
        var steps = new List<ShareProbeStep>();
        var total = Math.Max(source.TotalBytes, 1);

        steps.AddRange(from bucket in source.Buckets.OrderBy(b => b.Id)
            where bucket.FileCount > 0 && bucket.TotalBytes >= o.BucketFloorBytes
            let size = ClampMedian(bucket.Sizes.Median)
            let count = bucket.TotalBytes * 100 / total >= 20 ? 2 : 1
            select new ShareProbeStep
            {
                Workload = bucket.Name,
                Bucket = bucket.Id,
                ProbeBytes = size,
                ProbeCount = count,
                IsMetadata = false
            });

        if (NeedsMetadata(source))
        {
            steps.Add(new ShareProbeStep
            {
                Workload = "ManySmall",
                Bucket = null,
                ProbeBytes = MetadataFileBytes,
                ProbeCount = MetadataFileCount,
                IsMetadata = true
            });
        }

        FitBudget(steps, o.MaxProbeBytes);

        return new ShareProbePlan
        {
            Mode = ShareCampaignMode.Advanced,
            SourcePath = source.Path,
            PlannedBytes = planned,
            MaxProbeBytes = o.MaxProbeBytes,
            Probes = steps
        };
    }

    private static bool NeedsMetadata(FileIoDirectoryAnalysis source)
    {
        if (source.FileCount <= 0)
            return false;

        var tinySmallFiles = CountFiles(source, FileIoBucket.Tiny) + CountFiles(source, FileIoBucket.Small);
        var tinySmallBytes = CountBytes(source, FileIoBucket.Tiny) + CountBytes(source, FileIoBucket.Small);
        return tinySmallFiles * 100 / source.FileCount >= 80
               && tinySmallBytes * 100 / Math.Max(source.TotalBytes, 1) <= 20;
    }

    private static int CountFiles(FileIoDirectoryAnalysis source, FileIoBucket id)
        => source.Buckets.FirstOrDefault(b => b.Id == id)?.FileCount ?? 0;

    private static long CountBytes(FileIoDirectoryAnalysis source, FileIoBucket id)
        => source.Buckets.FirstOrDefault(b => b.Id == id)?.TotalBytes ?? 0;

    private static long ClampMedian(decimal? median)
    {
        var raw = median is { } m && m > 0 ? (long)decimal.Round(m, MidpointRounding.AwayFromZero) : MinProbeBytes;
        return Math.Clamp(raw, MinProbeBytes, MaxStepBytes);
    }

    private static void FitBudget(List<ShareProbeStep> steps, long maxBytes)
    {
        while (Cost(steps) > maxBytes && steps.Exists(s => !s.IsMetadata && s.ProbeCount > 1))
        {
            var i = steps.FindIndex(s => !s.IsMetadata && s.ProbeCount > 1);
            steps[i] = Clone(steps[i], count: 1);
        }

        while (Cost(steps) > maxBytes)
        {
            var drop = steps.FindIndex(s => !s.IsMetadata && s.Bucket is FileIoBucket.Tiny or FileIoBucket.Small or FileIoBucket.Medium);
            if (drop < 0)
                drop = steps.FindIndex(s => s.IsMetadata);
            if (drop < 0)
                break;
            steps.RemoveAt(drop);
        }
    }

    private static long Cost(IEnumerable<ShareProbeStep> steps)
        => steps.Sum(s => s.ProbeBytes * (long)s.ProbeCount);

    private static ShareProbeStep Clone(ShareProbeStep step, int count)
        => new()
        {
            Workload = step.Workload,
            Bucket = step.Bucket,
            ProbeBytes = step.ProbeBytes,
            ProbeCount = count,
            IsMetadata = step.IsMetadata
        };
}
