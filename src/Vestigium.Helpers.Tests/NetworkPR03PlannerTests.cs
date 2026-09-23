using Vestigium.Helpers.FileIo;
using Vestigium.Helpers.Network;

namespace Vestigium.Helpers.Tests;

public sealed class NetworkPR03PlannerTests
{
    [Fact]
    public void PR03_004_many_tiny_plus_huge_includes_metadata_and_huge()
    {
        var tinyBytes = 90L * 1024;
        var hugeBytes = 10L * 300L * 1024 * 1024;
        var analysis = Analysis(
            files: 100,
            total: tinyBytes + hugeBytes,
            Bucket(FileIoBucket.Tiny, 90, tinyBytes, 1024),
            Bucket(FileIoBucket.Huge, 10, hugeBytes, 300L * 1024 * 1024));

        var plan = NetworkHelper.PlanShareProbe(analysis);
        Assert.Contains(plan.Probes, p => p.IsMetadata && p.Workload == "ManySmall" && p.ProbeCount == 256 && p.ProbeBytes == 4096);
        Assert.Contains(plan.Probes, p => p.Bucket == FileIoBucket.Huge && p.ProbeBytes == 64L * 1024 * 1024);
        Assert.Equal(tinyBytes + hugeBytes, plan.PlannedBytes);
    }

    [Fact]
    public void PR03_004_only_large_skips_metadata()
    {
        var one = 80L * 1024 * 1024;
        var analysis = Analysis(
            files: 20,
            total: 20 * one,
            Bucket(FileIoBucket.Large, 20, 20 * one, one));

        var plan = NetworkHelper.PlanShareProbe(analysis);
        Assert.DoesNotContain(plan.Probes, p => p.IsMetadata);
        var large = Assert.Single(plan.Probes);
        Assert.Equal(FileIoBucket.Large, large.Bucket);
        Assert.Equal(64L * 1024 * 1024, large.ProbeBytes);
        Assert.Equal(2, large.ProbeCount);
    }

    private static FileIoDirectoryAnalysis Analysis(int files, long total, params FileIoBucketCensus[] buckets)
        => new()
        {
            Path = "source",
            FileCount = files,
            DirectoryCount = 1,
            TotalBytes = total,
            TotalSize = FileIoSize.FromBytes(total),
            FileSizes = FileIoSeriesSnapshot.From([total], "file-size-bytes", "bytes"),
            Buckets = buckets
        };

    private static FileIoBucketCensus Bucket(FileIoBucket id, int files, long bytes, long median)
        => new()
        {
            Id = id,
            Name = id.ToString(),
            FileCount = files,
            TotalBytes = bytes,
            Sizes = FileIoSeriesSnapshot.From(Enumerable.Repeat((double)median, Math.Max(files, 1)), $"file-size-bytes.{id}", "bytes")
        };
}
