using Vestigium.Helpers.Analytics;
using Vestigium.Helpers.ClosedXml;
using Vestigium.Helpers.Encryption;
using Vestigium.Helpers.FileIo;
using Vestigium.Helpers.Json;
using Vestigium.Helpers.Network;
using Vestigium.Helpers.Processes;
using Vestigium.Helpers.Services;
using Vestigium.Helpers.WinReg;
using Vestigium.Helpers.Xml;
using Vestigium.Logging;

namespace Vestigium.Helpers.Tests;

[Collection("Logger")]
public sealed class HelperLogTests
{
    public HelperLogTests()
    {
        HelperLog.Shutdown();
    }

    [Fact]
    public void Write_before_initialize_does_not_throw()
    {
        HelperLog.Information(HelperLog.AppIds.ClosedXml, VestigiumStatus.Success, "ClosedXml", "silent");
        Assert.False(HelperLog.IsInitialized);
        Assert.Empty(HelperLog.RecentJsonLines);
    }

    [Theory]
    [InlineData(HelperLog.AppIds.Core)]
    [InlineData(HelperLog.AppIds.ClosedXml)]
    [InlineData(HelperLog.AppIds.Encryption)]
    [InlineData(HelperLog.AppIds.WinReg)]
    [InlineData(HelperLog.AppIds.Json)]
    [InlineData(HelperLog.AppIds.Xml)]
    [InlineData(HelperLog.AppIds.FileIo)]
    [InlineData(HelperLog.AppIds.Processes)]
    [InlineData(HelperLog.AppIds.Services)]
    [InlineData(HelperLog.AppIds.Analytics)]
    [InlineData(HelperLog.AppIds.Network)]
    public void Probe_writes_jsonl_with_helper_appid(string appId)
    {
        var dir = Path.Combine(Path.GetTempPath(), "VestigiumHelpersTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        HelperLog.InitializeHost(appId, cfg => cfg.LogDirectory = dir);
        try
        {
            var identity = Probe(appId);
            Assert.StartsWith("Vestigium.Helpers", identity);
            var lines = HelperLog.RecentJsonLines;
            Assert.NotEmpty(lines);
            Assert.Contains(lines, l => l.Contains($"\"APPID\":\"{appId}\""));
            Assert.Contains(lines, l => l.Contains("\"CATEGORY\":\"Helpers\""));
            Assert.Contains(lines, l => l.Contains("\"STATUS\":\"Pending\""));
            Assert.Contains(lines, l => l.Contains("\"STATUS\":\"Success\""));
            Assert.Equal(dir, HelperLog.LogDirectory);
        }
        finally
        {
            HelperLog.Shutdown();
        }
    }

    [Fact]
    public void Each_cli_appid_is_registered_in_taxonomy()
    {
        foreach (var id in HelperLog.AllAppIds)
            Assert.True(HelperLog.Taxonomy.IsSubcategoryRegistered(HelperLog.Category, id), id);
    }

    private static string Probe(string appId) => appId switch
    {
        HelperLog.AppIds.Core => HelperGuard.Probe(),
        HelperLog.AppIds.ClosedXml => WorkbookHelper.Probe(),
        HelperLog.AppIds.Encryption => EncryptionHelper.Probe(),
        HelperLog.AppIds.WinReg => RegistryHelper.Probe(),
        HelperLog.AppIds.Json => JsonHelper.Probe(),
        HelperLog.AppIds.Xml => XmlHelper.Probe(),
        HelperLog.AppIds.FileIo => FileIoHelper.Probe(),
        HelperLog.AppIds.Processes => ProcessHelper.Probe(),
        HelperLog.AppIds.Services => ServiceHelper.Probe(),
        HelperLog.AppIds.Analytics => AnalyticsHelper.Probe(),
        HelperLog.AppIds.Network => NetworkHelper.Probe(),
        _ => throw new ArgumentOutOfRangeException(nameof(appId), appId, "Unknown helper APPID.")
    };
}

[CollectionDefinition("Logger", DisableParallelization = true)]
public sealed class LoggerCollection;
