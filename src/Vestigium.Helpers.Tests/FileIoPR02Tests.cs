using Vestigium.Helpers.Analytics;
using Vestigium.Helpers.FileIo;
using Vestigium.Logging;

namespace Vestigium.Helpers.Tests;

[Collection("Logger")]
public sealed class FileIoPR02Tests
{
    public FileIoPR02Tests()
    {
        VestigiumLogger.Shutdown();
    }

    private static string Init()
    {
        var dir = Path.Combine(Path.GetTempPath(), "VestigiumFileIoPR02", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        VestigiumLogger.Initialize(cfg =>
        {
            cfg.AppId = FileIoCatalog.AppId;
            cfg.LogDirectory = dir;
            cfg.MinimumDiskLevel = VestigiumLogLevel.Debug;
            cfg.OperationsLogEnabled = false;
            FileIoCatalog.Register(cfg);
            AnalyticsCatalog.Register(cfg);
        });
        return dir;
    }

    [Fact]
    public void Catalog_register_has_23_rows_including_12535()
    {
        Assert.Equal(23, FileIoCatalog.Rows.Length);
        Assert.Contains(FileIoCatalog.Rows, row => row.EventId == FileIoEvents.JobStart);
        Assert.Contains(FileIoCatalog.Rows, row => row.EventId == FileIoEvents.StatsFinalize);
        Assert.Contains(FileIoCatalog.Rows, row => row.EventId == FileIoEvents.JobCancelled);
        var dir = Path.Combine(Path.GetTempPath(), "VestigiumFileIoPR02", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        VestigiumLogger.Initialize(cfg =>
        {
            cfg.AppId = FileIoCatalog.AppId;
            cfg.LogDirectory = dir;
            FileIoCatalog.Register(cfg);
        });
        VestigiumLogger.Shutdown();
    }

    [Fact]
    public async Task Copy_job_writes_12535_then_12600_then_12540_same_correlation()
    {
        var root = Path.Combine(Path.GetTempPath(), "VestigiumFileIoPR02Job", Guid.NewGuid().ToString("N"));
        var src = Path.Combine(root, "src");
        var dest = Path.Combine(root, "dest");
        Directory.CreateDirectory(src);
        File.WriteAllText(Path.Combine(src, "a.txt"), "ok");
        try
        {
            Init();
            var job = FileIoJob.Copy(src, dest, new FileIoJobOptions { AuditMode = true });
            Assert.StartsWith("fio-", job.JobId);
            Assert.Equal(16, job.JobId.Length);
            var result = await job.RunAsync();
            Assert.Equal("Success", result.Status);
            VestigiumLogger.Flush();
            var lines = VestigiumLogger.RecentJsonLines.ToArray();
            Assert.Contains(lines, line => line.Contains("\"EVENTID\":12535", StringComparison.Ordinal));
            Assert.Contains(lines, line => line.Contains("\"EVENTID\":12600", StringComparison.Ordinal));
            Assert.Contains(lines, line => line.Contains("\"EVENTID\":12540", StringComparison.Ordinal));
            var start = lines.First(line => line.Contains("\"EVENTID\":12535", StringComparison.Ordinal));
            Assert.Contains(job.JobId, start, StringComparison.Ordinal);
        }
        finally
        {
            VestigiumLogger.Shutdown();
            try { Directory.Delete(root, true); } catch (IOException) { }
        }
    }

    [Fact]
    public async Task Cancel_writes_12545()
    {
        var root = Path.Combine(Path.GetTempPath(), "VestigiumFileIoPR02Cancel", Guid.NewGuid().ToString("N"));
        var src = Path.Combine(root, "src");
        var dest = Path.Combine(root, "dest");
        Directory.CreateDirectory(src);
        File.WriteAllText(Path.Combine(src, "a.txt"), "ok");
        try
        {
            Init();
            var job = FileIoJob.Copy(src, dest, new FileIoJobOptions { AuditMode = true });
            job.Cancel();
            var result = await job.RunAsync();
            Assert.Equal("Cancelled", result.Status);
            VestigiumLogger.Flush();
            Assert.Contains(VestigiumLogger.RecentJsonLines, line => line.Contains("\"EVENTID\":12545", StringComparison.Ordinal));
        }
        finally
        {
            VestigiumLogger.Shutdown();
            try { Directory.Delete(root, true); } catch (IOException) { }
        }
    }

    [Fact]
    public async Task Copy_without_host_does_not_throw()
    {
        var root = Path.Combine(Path.GetTempPath(), "VestigiumFileIoPR02NoHost", Guid.NewGuid().ToString("N"));
        var src = Path.Combine(root, "src");
        var dest = Path.Combine(root, "dest");
        Directory.CreateDirectory(src);
        File.WriteAllText(Path.Combine(src, "a.txt"), "ok");
        try
        {
            var result = await FileIoJob.Copy(src, dest, new FileIoJobOptions { AuditMode = true }).RunAsync();
            Assert.Equal("Success", result.Status);
        }
        finally
        {
            try { Directory.Delete(root, true); } catch (IOException) { }
        }
    }
}
