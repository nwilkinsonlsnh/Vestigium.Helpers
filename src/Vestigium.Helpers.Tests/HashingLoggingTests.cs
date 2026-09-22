using Vestigium.Helpers.Hashing;
using Vestigium.Logging;

namespace Vestigium.Helpers.Tests;

[Collection("Logger")]
public sealed class HashingLoggingTests
{
    public HashingLoggingTests() => VestigiumLogger.Shutdown();

    private static string Init()
    {
        var dir = Path.Combine(Path.GetTempPath(), "VestigiumHashLog", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        VestigiumLogger.Initialize(cfg =>
        {
            cfg.AppId = HashingCatalog.AppId;
            cfg.LogDirectory = dir;
            cfg.MinimumDiskLevel = VestigiumLogLevel.Debug;
            cfg.OperationsLogEnabled = false;
            HashingCatalog.Register(cfg);
        });
        return dir;
    }

    [Fact]
    public void Probe_writes_13005()
    {
        try
        {
            Init();
            Assert.Equal("Vestigium.Helpers.Hashing", HashingHelper.Probe());
            VestigiumLogger.Flush();
            Assert.Contains(VestigiumLogger.RecentJsonLines, line => line.Contains("\"EVENTID\":13005"));
        }
        finally
        {
            VestigiumLogger.Shutdown();
        }
    }

    [Fact]
    public void Probe_without_host_does_not_throw()
    {
        Assert.Equal("Vestigium.Helpers.Hashing", HashingHelper.Probe());
    }

    [Fact]
    public void Catalog_rows_match_event_constants()
    {
        Assert.Contains(HashingCatalog.Rows, r => r.EventId == HashingEvents.HashComplete && r.Name == "HashComplete");
        Assert.Contains(HashingCatalog.Rows, r => r.EventId == HashingEvents.HashFileComplete && r.Name == "HashFileComplete");
        Assert.Contains(HashingCatalog.Rows, r => r.EventId == HashingEvents.HmacComplete && r.Name == "HmacComplete");
        Assert.Contains(HashingCatalog.Rows, r => r.EventId == HashingEvents.PasswordHashed && r.Name == "PasswordHashed");
        Assert.Contains(HashingCatalog.Rows, r => r.EventId == HashingEvents.Rejected && r.Name == "Rejected");
        Assert.Equal(15, HashingCatalog.Rows.Length);
    }

    [Fact]
    public void HashString_jsonl_is_13025_without_digest()
    {
        try
        {
            Init();
            var digest = HashingHelper.HashString("abc");
            Assert.Equal(64, digest.Length);
            VestigiumLogger.Flush();
            Assert.Contains(VestigiumLogger.RecentJsonLines, line => line.Contains("\"EVENTID\":13025"));
            Assert.DoesNotContain(VestigiumLogger.RecentJsonLines, line => line.Contains(digest));
        }
        finally
        {
            VestigiumLogger.Shutdown();
        }
    }

    [Fact]
    public void HashFile_jsonl_is_13030_and_may_include_digest()
    {
        try
        {
            Init();
            var dir = Path.Combine(Path.GetTempPath(), "VestigiumHashLog", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(dir);
            var path = Path.Combine(dir, "payload.bin");
            File.WriteAllText(path, "abc");
            var digest = HashingHelper.HashFile(path);
            VestigiumLogger.Flush();
            Assert.Contains(VestigiumLogger.RecentJsonLines, line => line.Contains("\"EVENTID\":13030"));
            Assert.Contains(VestigiumLogger.RecentJsonLines, line => line.Contains("digest=" + digest));
        }
        finally
        {
            VestigiumLogger.Shutdown();
        }
    }
}
