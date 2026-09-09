using Vestigium.Helpers.FileIo;
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
