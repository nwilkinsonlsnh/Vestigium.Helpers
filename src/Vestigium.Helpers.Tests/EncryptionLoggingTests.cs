using Vestigium.Helpers.Encryption;
using Vestigium.Logging;

namespace Vestigium.Helpers.Tests;

[Collection("Logger")]
public sealed class EncryptionLoggingTests
{
    public EncryptionLoggingTests()
    {
        VestigiumLogger.Shutdown();
    }

    private static string Init()
    {
        var dir = Path.Combine(Path.GetTempPath(), "VestigiumEncLog", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        VestigiumLogger.Initialize(cfg =>
        {
            cfg.AppId = EncryptionCatalog.AppId;
            cfg.LogDirectory = dir;
            cfg.MinimumDiskLevel = VestigiumLogLevel.Debug;
            cfg.OperationsLogEnabled = false;
            EncryptionCatalog.Register(cfg);
        });
        return dir;
    }

    [Fact]
    public void Probe_writes_12005()
    {
        try
        {
            Init();
            var id = EncryptionHelper.Probe();
            Assert.Equal("Vestigium.Helpers.Encryption", id);
            VestigiumLogger.Flush();
            Assert.Contains(VestigiumLogger.RecentJsonLines, line => line.Contains("\"EVENTID\":12005"));
        }
        finally
        {
            VestigiumLogger.Shutdown();
        }
    }

    [Fact]
    public void SealFile_missing_source_writes_12030()
    {
        try
        {
            var dir = Init();
            using var secret = EncryptionSecret.FromPassphrase("unit-test-only");
            var missing = Path.Combine(dir, "no-such.bin");
            var dest = Path.Combine(dir, "out.aes");
            Assert.Throws<FileNotFoundException>(() => EncryptionHelper.SealFile(missing, dest, secret));
            VestigiumLogger.Flush();
            Assert.Contains(VestigiumLogger.RecentJsonLines, line => line.Contains("\"EVENTID\":12030"));
        }
        finally
        {
            VestigiumLogger.Shutdown();
        }
    }

    [Fact]
    public void Probe_without_host_does_not_throw()
    {
        Assert.Equal("Vestigium.Helpers.Encryption", EncryptionHelper.Probe());
    }
}
