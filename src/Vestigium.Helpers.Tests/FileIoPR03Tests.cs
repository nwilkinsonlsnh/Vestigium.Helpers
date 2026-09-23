using Vestigium.Helpers.FileIo;

namespace Vestigium.Helpers.Tests;

public sealed class FileIoPR03Tests
{
    [Fact]
    public void UniqueName_dot_hash_hash_then_01_then_03()
    {
        Assert.Equal("report.01.txt", UniqueName.Next(["report.txt"], "report.txt", ".##"));
        Assert.Equal("report.03.txt", UniqueName.Next(["report.txt", "report.01.txt", "report.02.txt"], "report.txt", ".##"));
    }

    [Fact]
    public void NameCap_does_not_overwrite_original_dest()
    {
        Assert.Null(UniqueName.Next(
            [.. Enumerable.Range(0, 100).Select(i => i == 0 ? "report.txt" : $"report.{i:00}.txt")],
            "report.txt",
            ".##"));

        var (src, dst) = Tree();
        File.WriteAllText(Path.Combine(src, "report.txt"), "new");
        File.WriteAllText(Path.Combine(dst, "report.txt"), "old");
        for (var i = 1; i <= 9; i++)
            File.WriteAllText(Path.Combine(dst, $"report.{i}.txt"), i.ToString());
        var result = FileIoHelper.Copy(src, dst, new FileIoJobOptions
        {
            ReconLeadTime = TimeSpan.Zero,
            Collision = FileIoCollision.UniqueName,
            UniqueNamePattern = ".#",
        }).RunAsync().GetAwaiter().GetResult();
        Assert.True(result.Failed >= 1, result.Status);
        Assert.Equal("old", File.ReadAllText(Path.Combine(dst, "report.txt")));
    }

    [Fact]
    public async Task Audit_mode_creates_no_dest_files()
    {
        var (src, dst) = Tree();
        File.WriteAllText(Path.Combine(src, "a.txt"), "payload");
        File.WriteAllText(Path.Combine(dst, "held.txt"), "kept");
        var before = Directory.GetFiles(dst).Length;
        var result = await FileIoHelper.Copy(src, dst, new FileIoJobOptions
        {
            ReconLeadTime = TimeSpan.Zero,
            AuditMode = true,
        }).RunAsync();
        Assert.True(result.AuditMode);
        Assert.Equal("Success", result.Status);
        Assert.Equal(before, Directory.GetFiles(dst).Length);
        Assert.False(File.Exists(Path.Combine(dst, "a.txt")));
        Assert.Equal("kept", File.ReadAllText(Path.Combine(dst, "held.txt")));
    }

    [Fact]
    public async Task Cancel_does_not_delete_source_and_does_not_leave_a_complete_dest()
    {
        var (src, dst) = Tree();
        var from = Path.Combine(src, "big.bin");
        File.WriteAllBytes(from, new byte[4 * 1024 * 1024]);
        var job = FileIoHelper.Copy(src, dst, new FileIoJobOptions { ReconLeadTime = TimeSpan.Zero });
        var run = job.RunAsync();
        job.Cancel();
        var result = await run;
        Assert.Equal("Cancelled", result.Status);
        Assert.True(File.Exists(from));
        var dest = Path.Combine(dst, "big.bin");
        if (File.Exists(dest))
            Assert.True(new FileInfo(dest).Length < 4 * 1024 * 1024);
    }

    [Fact]
    public void ReconLeadTime_above_180_throws()
    {
        var (src, dst) = Tree();
        File.WriteAllText(Path.Combine(src, "a.txt"), "x");
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            FileIoHelper.Copy(src, dst, new FileIoJobOptions
            {
                ReconLeadTime = TimeSpan.FromSeconds(181),
            }));
    }

    [Fact]
    public async Task Empty_stats_bins_are_count_zero_and_series_null()
    {
        var (src, dst) = Tree();
        File.WriteAllText(Path.Combine(src, "tiny.txt"), "x");
        var result = await FileIoHelper.Copy(src, dst, new FileIoJobOptions
        {
            ReconLeadTime = TimeSpan.Zero,
            AuditMode = true,
        }).RunAsync();
        Assert.NotNull(result.Stats);
        var huge = result.Stats!.Buckets[(int)FileIoBucket.Huge];
        Assert.Equal(0, huge.FileSizes.Count);
        Assert.Null(huge.FileSizes.Series);
        Assert.Equal(0, huge.TransferRates.Count);
        Assert.Null(huge.TransferRates.Series);
        Assert.Equal(0, result.Stats.TransferRates.Count);
        Assert.Null(result.Stats.TransferRates.Series);
    }

    [Fact]
    public async Task Locked_dest_is_InUse_and_other_files_continue()
    {
        var (src, dst) = Tree();
        File.WriteAllText(Path.Combine(src, "a.txt"), "payload-a");
        File.WriteAllText(Path.Combine(src, "b.txt"), "payload-b");
        Directory.CreateDirectory(Path.Combine(dst, "a.txt"));
        var result = await FileIoHelper.Copy(src, dst, new FileIoJobOptions
        {
            ReconLeadTime = TimeSpan.Zero,
            RetryCount = 0,
            RetryWait = TimeSpan.FromMilliseconds(1),
        }).RunAsync();
        Assert.True(result.Failed >= 1, result.Status);
        Assert.True(File.Exists(Path.Combine(dst, "b.txt")));
        Assert.Equal("payload-b", File.ReadAllText(Path.Combine(dst, "b.txt")));
        Assert.True(Directory.Exists(Path.Combine(dst, "a.txt")));
    }

    [Fact]
    public async Task StopOnError_cancels_after_InUse()
    {
        var (src, dst) = Tree();
        File.WriteAllText(Path.Combine(src, "a.txt"), "a");
        File.WriteAllText(Path.Combine(src, "b.txt"), "b");
        Directory.CreateDirectory(Path.Combine(dst, "a.txt"));
        var result = await FileIoHelper.Copy(src, dst, new FileIoJobOptions
        {
            ReconLeadTime = TimeSpan.Zero,
            Collision = FileIoCollision.Overwrite,
            StopOnError = true,
            RetryCount = 0,
            RetryWait = TimeSpan.FromMilliseconds(1),
        }).RunAsync();
        Assert.Equal("Cancelled", result.Status);
        Assert.True(result.Failed >= 1);
    }

    [Fact]
    public void CleanIndexesOlderThan_uses_injected_temp_root()
    {
        var root = Path.Combine(Path.GetTempPath(), "VestigiumFileIoPR03Index", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        FileIoHelper.IndexRootOverride = root;
        try
        {
            var oldFile = Path.Combine(root, "old.jsonl");
            File.WriteAllText(oldFile, "{}");
            File.SetLastWriteTimeUtc(oldFile, DateTime.UtcNow.AddDays(-2));
            var keep = Path.Combine(root, "keep.jsonl");
            File.WriteAllText(keep, "{}");
            FileIoHelper.CleanIndexesOlderThan(TimeSpan.FromHours(12));
            Assert.False(File.Exists(oldFile));
            Assert.True(File.Exists(keep));
            Assert.StartsWith(Path.GetTempPath().TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar), FileIoHelper.IndexRoot(), StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            FileIoHelper.IndexRootOverride = null;
        }
    }

    private static (string Src, string Dst) Tree()
    {
        var root = Path.Combine(Path.GetTempPath(), "VestigiumFileIoPR03", Guid.NewGuid().ToString("N"));
        var src = Path.Combine(root, "src");
        var dst = Path.Combine(root, "dst");
        Directory.CreateDirectory(src);
        Directory.CreateDirectory(dst);
        return (src, dst);
    }
}
