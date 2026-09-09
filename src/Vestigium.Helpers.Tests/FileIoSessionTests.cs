using Vestigium.Helpers.FileIo;
using Vestigium.Helpers.Hashing;
using Vestigium.Logging;

namespace Vestigium.Helpers.Tests;

[Collection("Logger")]
public sealed class FileIoSessionTests
{
    public FileIoSessionTests()
    {
        HelperLog.Shutdown();
    }

    [Fact]
    public void Identity_is_stable()
        => Assert.Equal("Vestigium.Helpers.FileIo", FileIoHelper.Identity);

    [Fact]
    public void Probe_is_temp_only_and_logs_pending_then_success()
    {
        var dir = TempDir();
        HelperLog.InitializeHost(HelperLog.AppIds.FileIo, cfg => cfg.LogDirectory = dir);
        try
        {
            Assert.Equal("Vestigium.Helpers.FileIo", FileIoHelper.Probe());
            var lines = HelperLog.RecentJsonLines;
            Assert.Contains(lines, l => l.Contains("\"STATUS\":\"Pending\""));
            Assert.Contains(lines, l => l.Contains("\"STATUS\":\"Success\""));
            Assert.Contains(lines, l => l.Contains("\"APPID\":\"FileIo\""));
            Assert.DoesNotContain(lines, l => l.Contains("BEGIN "));
        }
        finally
        {
            HelperLog.Shutdown();
        }
    }

    [Fact]
    public void UniqueName_report_txt_then_01_then_03()
    {
        Assert.Equal("report.01.txt", UniqueName.Next(["report.txt"], "report.txt", ".##"));
        Assert.Equal("report.03.txt", UniqueName.Next(["report.txt", "report.01.txt", "report.02.txt"], "report.txt", ".##"));
    }

    [Fact]
    public void UniqueName_cap_at_99_does_not_overwrite()
        => Assert.Null(UniqueName.Next(
            Enumerable.Range(0, 100).Select(i => i == 0 ? "report.txt" : $"report.{i:00}.txt").ToArray(),
            "report.txt",
            ".##"));

    [Fact]
    public void UniqueName_A_width_stays_two_digits()
    {
        Assert.Equal("report.A01.txt", UniqueName.Next(Array.Empty<string>(), "report.txt", "A##"));
        Assert.Equal("report.A02.txt", UniqueName.Next(["report.A01.txt"], "report.txt", "A##"));
        Assert.Equal("report.B01.txt", UniqueName.Next(["report.A99.txt"], "report.txt", "A##"));
    }

    [Fact]
    public void Lead_time_181_seconds_is_rejected()
    {
        var root = TempDir();
        File.WriteAllText(Path.Combine(root, "a.txt"), "x");
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            FileIoHelper.Copy(Path.Combine(root, "a.txt"), Path.Combine(root, "b.txt"), new FileIoJobOptions
            {
                ReconLeadTime = TimeSpan.FromSeconds(181),
            }));
    }

    [Fact]
    public async Task Copy_UniqueName_keeps_dest_and_writes_01()
    {
        var (src, dst) = Tree();
        File.WriteAllText(Path.Combine(src, "nathan.txt"), "nathan capture\n");
        File.WriteAllText(Path.Combine(dst, "nathan.txt"), "older nathan\n");
        var job = FileIoHelper.Copy(src, dst, new FileIoJobOptions { ReconLeadTime = TimeSpan.Zero });
        var result = await job.RunAsync();
        Assert.Equal("Success", result.Status);
        Assert.Equal("older nathan\n", File.ReadAllText(Path.Combine(dst, "nathan.txt")));
        Assert.Equal("nathan capture\n", File.ReadAllText(Path.Combine(dst, "nathan.01.txt")));
        Assert.NotNull(result.Stats);
        Assert.True(result.Stats!.FileSizes.Count >= 1);
        Assert.NotNull(result.Stats.FileSizes.Median);
        Assert.True(result.Stats.TransferRates.Count >= 1);
        Assert.NotNull(result.Stats.FileSizes.Series);
    }

    [Fact]
    public async Task Analytics_describes_sizes_per_bucket_and_omits_rates_in_audit()
    {
        var (src, dst) = Tree();
        File.WriteAllText(Path.Combine(src, "tiny.txt"), "x");
        File.WriteAllBytes(Path.Combine(src, "small.bin"), new byte[300 * 1024]);
        File.WriteAllBytes(Path.Combine(src, "medium.bin"), new byte[5 * 1024 * 1024]);
        var live = await FileIoHelper.Copy(src, dst, new FileIoJobOptions { ReconLeadTime = TimeSpan.Zero }).RunAsync();
        Assert.Equal("Success", live.Status);
        Assert.NotNull(live.Stats);
        Assert.True(live.Stats!.FileSizes.Count >= 3);
        Assert.True(live.Stats.Buckets.Count(b => b.FileSizes.Count > 0) >= 3);
        Assert.True(live.Stats.TransferRates.Count >= 1);
        Assert.NotNull(live.Stats.FileSizes.P95);
        Assert.True(live.Stats.FileSizes.Mean is > 0);

        var audit = await FileIoHelper.Copy(src, dst, new FileIoJobOptions
        {
            ReconLeadTime = TimeSpan.Zero,
            AuditMode = true,
        }).RunAsync();
        Assert.True(audit.Stats!.FileSizes.Count >= 3);
        Assert.Equal(0, audit.Stats.TransferRates.Count);
        Assert.Null(audit.Stats.TransferRates.Series);
    }

    [Fact]
    public async Task Skip_does_not_replace_Overwrite_does()
    {
        var (src, dst) = Tree();
        File.WriteAllText(Path.Combine(src, "nathan.txt"), "nathan capture\n");
        File.WriteAllText(Path.Combine(dst, "nathan.txt"), "older nathan\n");
        await FileIoHelper.Copy(Path.Combine(src, "nathan.txt"), Path.Combine(dst, "nathan.txt"), new FileIoJobOptions
        {
            ReconLeadTime = TimeSpan.Zero,
            Collision = FileIoCollision.Skip,
        }).RunAsync();
        Assert.Equal("older nathan\n", File.ReadAllText(Path.Combine(dst, "nathan.txt")));
        await FileIoHelper.Copy(Path.Combine(src, "nathan.txt"), Path.Combine(dst, "nathan.txt"), new FileIoJobOptions
        {
            ReconLeadTime = TimeSpan.Zero,
            Collision = FileIoCollision.Overwrite,
        }).RunAsync();
        Assert.Equal("nathan capture\n", File.ReadAllText(Path.Combine(dst, "nathan.txt")));
    }

    [Fact]
    public async Task Audit_mode_does_not_mutate_dest()
    {
        var (src, dst) = Tree();
        File.WriteAllText(Path.Combine(src, "nathan.txt"), "nathan capture\n");
        File.WriteAllText(Path.Combine(dst, "held.txt"), "kept\n");
        var before = Directory.GetFiles(dst).Length;
        var result = await FileIoHelper.Copy(src, dst, new FileIoJobOptions
        {
            ReconLeadTime = TimeSpan.Zero,
            AuditMode = true,
        }).RunAsync();
        Assert.True(result.AuditMode);
        Assert.Equal(before, Directory.GetFiles(dst).Length);
        Assert.Equal("kept\n", File.ReadAllText(Path.Combine(dst, "held.txt")));
    }

    [Fact]
    public async Task Unique_content_skip_duplicate()
    {
        var (src, dst) = Tree();
        File.WriteAllText(Path.Combine(src, "a.txt"), "same-bytes");
        File.WriteAllText(Path.Combine(src, "b.txt"), "same-bytes");
        File.WriteAllText(Path.Combine(dst, "held.txt"), "same-bytes");
        var result = await FileIoHelper.Copy(src, dst, new FileIoJobOptions
        {
            ReconLeadTime = TimeSpan.Zero,
            CopyOnlyUniqueContent = true,
        }).RunAsync();
        Assert.True(result.Skipped >= 2);
    }

    [Fact]
    public async Task Delete_removes_the_file()
    {
        var root = TempDir();
        var file = Path.Combine(root, "gone.txt");
        File.WriteAllText(file, "x");
        var result = await FileIoHelper.Delete(file, new FileIoJobOptions { ReconLeadTime = TimeSpan.Zero }).RunAsync();
        Assert.Equal("Success", result.Status);
        Assert.False(File.Exists(file));
    }

    [Fact]
    public async Task Move_copies_then_removes_source()
    {
        var (src, dst) = Tree();
        var from = Path.Combine(src, "session.jsonl");
        File.WriteAllText(from, "{\"STATUS\":\"Pending\"}\n");
        var to = Path.Combine(dst, "session.jsonl");
        await FileIoHelper.Move(from, to, new FileIoJobOptions
        {
            ReconLeadTime = TimeSpan.Zero,
            Collision = FileIoCollision.Overwrite,
        }).RunAsync();
        Assert.False(File.Exists(from));
        Assert.True(File.Exists(to));
    }

    [Fact]
    public async Task Mirror_purge_removes_dest_extras()
    {
        var (src, dst) = Tree();
        File.WriteAllText(Path.Combine(src, "keep.txt"), "keep");
        File.WriteAllText(Path.Combine(dst, "keep.txt"), "keep");
        File.WriteAllText(Path.Combine(dst, "extra.txt"), "gone");
        await FileIoHelper.Mirror(src, dst, new FileIoJobOptions
        {
            ReconLeadTime = TimeSpan.Zero,
            Purge = true,
            Collision = FileIoCollision.Overwrite,
        }).RunAsync();
        Assert.False(File.Exists(Path.Combine(dst, "extra.txt")));
        Assert.True(File.Exists(Path.Combine(dst, "keep.txt")));
    }

    [Fact]
    public async Task Pause_then_resume_completes()
    {
        var (src, dst) = Tree();
        File.WriteAllBytes(Path.Combine(src, "mid.bin"), new byte[512 * 1024]);
        var job = FileIoHelper.Copy(src, dst, new FileIoJobOptions
        {
            ReconLeadTime = TimeSpan.Zero,
            Collision = FileIoCollision.Overwrite,
        });
        var run = job.RunAsync();
        job.Pause();
        Assert.True(job.IsPaused);
        await Task.Delay(30);
        job.Resume();
        var result = await run;
        Assert.Equal("Success", result.Status);
        Assert.True(File.Exists(Path.Combine(dst, "mid.bin")));
    }

    [Fact]
    public async Task Cancel_aborts_the_job()
    {
        var (src, dst) = Tree();
        File.WriteAllBytes(Path.Combine(src, "big.bin"), new byte[2 * 1024 * 1024]);
        var job = FileIoHelper.Copy(src, dst, new FileIoJobOptions { ReconLeadTime = TimeSpan.Zero });
        var run = job.RunAsync();
        job.Cancel();
        var result = await run;
        Assert.Equal("Cancelled", result.Status);
    }

    [Fact]
    public void Taxonomy_registers_fileio_subcategories()
    {
        Assert.True(HelperLog.Taxonomy.IsSubcategoryRegistered(HelperLog.Category, HelperLog.Subcategories.Job));
        Assert.True(HelperLog.Taxonomy.IsSubcategoryRegistered(HelperLog.Category, HelperLog.Subcategories.Recon));
        Assert.True(HelperLog.Taxonomy.IsSubcategoryRegistered(HelperLog.Category, HelperLog.Subcategories.Copy));
        Assert.True(HelperLog.Taxonomy.IsSubcategoryRegistered(HelperLog.Category, HelperLog.Subcategories.SecureDelete));
        Assert.True(HelperLog.Taxonomy.IsSubcategoryRegistered(HelperLog.Category, HelperLog.Subcategories.Stats));
    }

    [Fact]
    public void RequestedBy_secret_is_rejected()
    {
        var (src, dst) = Tree();
        File.WriteAllText(Path.Combine(src, "a.txt"), "x");
        Assert.Throws<ArgumentException>(() =>
            FileIoHelper.Copy(src, dst, new FileIoJobOptions
            {
                ReconLeadTime = TimeSpan.Zero,
                RequestedBy = "-----BEGIN PRIVATE KEY-----",
            }));
    }

    [Fact]
    public void RequestedBy_over_50_characters_is_rejected()
    {
        var (src, dst) = Tree();
        File.WriteAllText(Path.Combine(src, "a.txt"), "x");
        Assert.Throws<ArgumentException>(() =>
            FileIoHelper.Copy(src, dst, new FileIoJobOptions
            {
                ReconLeadTime = TimeSpan.Zero,
                RequestedBy = new string('n', 51),
            }));
    }

    [Fact]
    public async Task Copy_bytes_match_hashing_sha256()
    {
        var (src, dst) = Tree();
        var from = Path.Combine(src, "session.jsonl");
        File.WriteAllText(from, "{\"STATUS\":\"Pending\"}\n");
        var to = Path.Combine(dst, "session.jsonl");
        await FileIoHelper.Copy(from, to, new FileIoJobOptions
        {
            ReconLeadTime = TimeSpan.Zero,
            Collision = FileIoCollision.Overwrite,
        }).RunAsync();
        Assert.Equal(HashingHelper.HashFile(from), HashingHelper.HashFile(to));
    }

    [Fact]
    public async Task MaxDepth_1_does_not_copy_grandchildren()
    {
        var (src, dst) = Tree();
        File.WriteAllText(Path.Combine(src, "root.txt"), "root");
        var sub = Path.Combine(src, "nested");
        Directory.CreateDirectory(sub);
        File.WriteAllText(Path.Combine(sub, "child.txt"), "child");
        await FileIoHelper.Copy(src, dst, new FileIoJobOptions
        {
            ReconLeadTime = TimeSpan.Zero,
            MaxDepth = 1,
        }).RunAsync();
        Assert.True(File.Exists(Path.Combine(dst, "root.txt")));
        Assert.False(File.Exists(Path.Combine(dst, "nested", "child.txt")));
    }

    [Fact]
    public async Task IncludeEmptyDirectories_creates_empty_dest_dirs()
    {
        var (src, dst) = Tree();
        Directory.CreateDirectory(Path.Combine(src, "empty"));
        File.WriteAllText(Path.Combine(src, "held.txt"), "x");
        await FileIoHelper.Copy(src, dst, new FileIoJobOptions
        {
            ReconLeadTime = TimeSpan.Zero,
            IncludeEmptyDirectories = false,
        }).RunAsync();
        Assert.False(Directory.Exists(Path.Combine(dst, "empty")));
        await FileIoHelper.Copy(src, dst, new FileIoJobOptions
        {
            ReconLeadTime = TimeSpan.Zero,
            IncludeEmptyDirectories = true,
        }).RunAsync();
        Assert.True(Directory.Exists(Path.Combine(dst, "empty")));
    }

    [Fact]
    public async Task Certainty_is_100_and_progress_has_five_buckets()
    {
        var (src, dst) = Tree();
        File.WriteAllText(Path.Combine(src, "a.txt"), "x");
        var job = FileIoHelper.Copy(src, dst, new FileIoJobOptions { ReconLeadTime = TimeSpan.Zero });
        FileIoProgress? last = null;
        job.ProgressChanged += (_, p) => last = p;
        await job.RunAsync();
        Assert.True(job.Progress.ReconComplete);
        Assert.Equal(100, job.Progress.CertaintyPercent);
        Assert.Equal(5, job.Progress.Buckets.Length);
        Assert.NotNull(last);
        Assert.Equal(5, last!.Buckets.Length);
    }

    [Fact]
    public async Task Mirror_without_purge_leaves_dest_extras()
    {
        var (src, dst) = Tree();
        File.WriteAllText(Path.Combine(src, "keep.txt"), "keep");
        File.WriteAllText(Path.Combine(dst, "extra.txt"), "stay");
        await FileIoHelper.Mirror(src, dst, new FileIoJobOptions
        {
            ReconLeadTime = TimeSpan.Zero,
            Purge = false,
            Collision = FileIoCollision.Overwrite,
        }).RunAsync();
        Assert.True(File.Exists(Path.Combine(dst, "extra.txt")));
    }

    [Fact]
    public void SecureDelete_removes_file_and_does_not_log_payload()
    {
        var dir = TempDir();
        HelperLog.InitializeHost(HelperLog.AppIds.FileIo, cfg => cfg.LogDirectory = dir);
        try
        {
            var file = Path.Combine(dir, "gone.txt");
            File.WriteAllText(file, "secret-payload-bytes");
            FileIoHelper.SecureDelete(file, FileIoShredRecipe.ZeroRandomZero);
            Assert.False(File.Exists(file));
            var lines = HelperLog.RecentJsonLines;
            Assert.Contains(lines, l => l.Contains("\"SUBCATEGORY\":\"SecureDelete\""));
            Assert.DoesNotContain(lines, l => l.Contains("secret-payload-bytes"));
            Assert.DoesNotContain(lines, HasExceptionPayload);
        }
        finally
        {
            HelperLog.Shutdown();
        }
    }

    [Fact]
    public async Task Job_pending_includes_by_and_reason_and_jsonl_has_no_payload()
    {
        var (src, dst) = Tree();
        File.WriteAllText(Path.Combine(src, "nathan.txt"), "nathan capture\n");
        var dir = TempDir();
        HelperLog.InitializeHost(HelperLog.AppIds.FileIo, cfg => cfg.LogDirectory = dir);
        try
        {
            var result = await FileIoHelper.Copy(src, dst, new FileIoJobOptions
            {
                ReconLeadTime = TimeSpan.Zero,
                RequestedBy = "wilkinson",
                Reason = "export-archive",
            }).RunAsync();
            Assert.Equal("Success", result.Status);
            var lines = string.Join('\n', HelperLog.RecentJsonLines);
            Assert.Contains("by=wilkinson", lines);
            Assert.Contains("reason=export-archive", lines);
            Assert.Contains("Stats job=", lines);
            Assert.DoesNotContain("nathan capture", lines);
            Assert.DoesNotContain("\"EXCEPTION\":\"", lines);
            Assert.DoesNotContain("\"EXCEPTION\":{", lines);
            Assert.DoesNotContain("BEGIN ", lines);
        }
        finally
        {
            HelperLog.Shutdown();
        }
    }

    [Fact]
    public async Task Copy_collision_with_directory_is_InUse_and_other_files_continue()
    {
        var (src, dst) = Tree();
        File.WriteAllText(Path.Combine(src, "a.txt"), "payload-a");
        File.WriteAllText(Path.Combine(src, "b.txt"), "payload-b");
        Directory.CreateDirectory(Path.Combine(dst, "a.txt"));
        var dir = TempDir();
        HelperLog.InitializeHost(HelperLog.AppIds.FileIo, cfg => cfg.LogDirectory = dir);
        try
        {
            var result = await FileIoHelper.Copy(src, dst, new FileIoJobOptions
            {
                ReconLeadTime = TimeSpan.Zero,
                RetryCount = 0,
                RetryWait = TimeSpan.FromMilliseconds(1),
            }).RunAsync();
            Assert.True(result.Failed >= 1);
            Assert.True(File.Exists(Path.Combine(dst, "b.txt")));
            Assert.Contains(HelperLog.RecentJsonLines, l => l.Contains("reason=InUse") || l.Contains("Failed"));
            Assert.DoesNotContain(HelperLog.RecentJsonLines, HasExceptionPayload);
            Assert.DoesNotContain(HelperLog.RecentJsonLines, l => l.Contains("payload-a"));
        }
        finally
        {
            HelperLog.Shutdown();
        }
    }

    [Fact]
    public async Task Cancel_of_new_file_does_not_leave_a_complete_dest()
    {
        var (src, dst) = Tree();
        File.WriteAllBytes(Path.Combine(src, "big.bin"), new byte[4 * 1024 * 1024]);
        var job = FileIoHelper.Copy(src, dst, new FileIoJobOptions { ReconLeadTime = TimeSpan.Zero });
        var run = job.RunAsync();
        job.Cancel();
        var result = await run;
        Assert.Equal("Cancelled", result.Status);
        var dest = Path.Combine(dst, "big.bin");
        if (File.Exists(dest))
            Assert.True(new FileInfo(dest).Length < 4 * 1024 * 1024);
    }

    [Fact]
    public void CleanIndexesOlderThan_uses_injected_root()
    {
        var root = TempDir();
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
        }
        finally
        {
            FileIoHelper.IndexRootOverride = null;
        }
    }

    static bool HasExceptionPayload(string line)
        => line.Contains("\"EXCEPTION\":\"", StringComparison.Ordinal)
           || line.Contains("\"EXCEPTION\":{", StringComparison.Ordinal)
           || line.Contains("   at ", StringComparison.Ordinal);

    static (string Src, string Dst) Tree()
    {
        var root = TempDir();
        var src = Path.Combine(root, "Export");
        var dst = Path.Combine(root, "Archive");
        Directory.CreateDirectory(src);
        Directory.CreateDirectory(dst);
        return (src, dst);
    }

    static string TempDir()
    {
        var dir = Path.Combine(Path.GetTempPath(), "VestigiumFileIoTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        return dir;
    }
}
