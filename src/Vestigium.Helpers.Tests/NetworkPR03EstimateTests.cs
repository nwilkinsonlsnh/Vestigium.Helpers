using Vestigium.Helpers.FileIo;
using Vestigium.Helpers.Network;

namespace Vestigium.Helpers.Tests;

public sealed class NetworkPR03EstimateTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "pr03-est-" + Guid.NewGuid().ToString("N"));

    public NetworkPR03EstimateTests()
    {
        Directory.CreateDirectory(_root);
        NetworkTestHooks.CampaignRoot = _root;
        NetworkTestHooks.ShareRoot = _root;
    }

    public void Dispose()
    {
        NetworkTestHooks.Reset();
        try { Directory.Delete(_root, true); } catch { }
    }

    [Fact]
    public void PR03_005_estimate_splits_payload_and_metadata()
    {
        var tinyBytes = 90L * 1024;
        var hugeBytes = 10L * 300L * 1024 * 1024;
        var analysis = new FileIoDirectoryAnalysis
        {
            Path = "source",
            FileCount = 100,
            DirectoryCount = 1,
            TotalBytes = tinyBytes + hugeBytes,
            TotalSize = FileIoSize.FromBytes(tinyBytes + hugeBytes),
            FileSizes = FileIoSeriesSnapshot.From([(double)(tinyBytes + hugeBytes)], "file-size-bytes", "bytes"),
            Buckets =
            [
                Census(FileIoBucket.Tiny, 90, tinyBytes, 1024),
                Census(FileIoBucket.Huge, 10, hugeBytes, 300L * 1024 * 1024)
            ]
        };

        NetworkTestHooks.ProbeRatesByWorkload = new Dictionary<string, IReadOnlyList<double>>(StringComparer.OrdinalIgnoreCase)
        {
            ["Huge"] = [100_000_000d],
            ["ManySmall"] = [409_600d]
        };

        var share = Path.Combine(_root, "share");
        Directory.CreateDirectory(share);
        var result = NetworkHelper.CreateShareCampaign(new ShareCampaignOptions
        {
            Target = new FileShareTarget { Directory = share },
            Mode = ShareCampaignMode.Advanced,
            SourceAnalysis = analysis,
            ResultsPath = Path.Combine(_root, "adv.jsonl")
        }).RunAsync().GetAwaiter().GetResult();

        Assert.Equal(ShareCampaignMode.Advanced, result.Mode);
        Assert.True(result.PayloadDuration > TimeSpan.Zero);
        Assert.True(result.MetadataDuration > TimeSpan.Zero);
        Assert.Equal(result.PayloadDuration + result.MetadataDuration, result.MeasuredDuration);
        Assert.Contains("payload", result.Disclaimer, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("metadata", result.Disclaimer, StringComparison.OrdinalIgnoreCase);

        var expectedPayload = TimeSpan.FromSeconds(tinyBytes / 100_000_000d + hugeBytes / 100_000_000d);
        var expectedMeta = TimeSpan.FromSeconds(4096d / 409_600d * 90);
        Assert.Equal(expectedPayload.TotalSeconds, result.PayloadDuration.TotalSeconds, 3);
        Assert.Equal(expectedMeta.TotalSeconds, result.MetadataDuration.TotalSeconds, 3);
    }

    private static FileIoBucketCensus Census(FileIoBucket id, int files, long bytes, long median)
        => new()
        {
            Id = id,
            Name = id.ToString(),
            FileCount = files,
            TotalBytes = bytes,
            Sizes = FileIoSeriesSnapshot.From(Enumerable.Repeat((double)median, Math.Max(files, 1)), $"sz.{id}", "bytes")
        };
}
