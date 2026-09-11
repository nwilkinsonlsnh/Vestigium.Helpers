using Vestigium.Helpers.Analytics;
using Vestigium.Helpers.ClosedXml;
using Vestigium.Helpers.Csv;
using Vestigium.Helpers.Encryption;
using Vestigium.Helpers.FileIo;
using Vestigium.Helpers.Hashing;
using Vestigium.Helpers.Json;
using Vestigium.Helpers.Kql;
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
    [InlineData(HelperLog.AppIds.Hashing)]
    [InlineData(HelperLog.AppIds.WinReg)]
    [InlineData(HelperLog.AppIds.Json)]
    [InlineData(HelperLog.AppIds.Xml)]
    [InlineData(HelperLog.AppIds.FileIo)]
    [InlineData(HelperLog.AppIds.Processes)]
    [InlineData(HelperLog.AppIds.Services)]
    [InlineData(HelperLog.AppIds.Analytics)]
    [InlineData(HelperLog.AppIds.Network)]
    [InlineData(HelperLog.AppIds.Csv)]
    [InlineData(HelperLog.AppIds.Kql)]
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

    [Fact]
    public void FileIo_subcategories_are_registered()
    {
        string[] required =
        [
            HelperLog.Subcategories.Probe,
            HelperLog.Subcategories.Identity,
            HelperLog.Subcategories.Guard,
            HelperLog.Subcategories.Job,
            HelperLog.Subcategories.Recon,
            HelperLog.Subcategories.Copy,
            HelperLog.Subcategories.Move,
            HelperLog.Subcategories.Delete,
            HelperLog.Subcategories.Mirror,
            HelperLog.Subcategories.Index,
            HelperLog.Subcategories.Progress,
            HelperLog.Subcategories.Compare,
            HelperLog.Subcategories.Prune,
            HelperLog.Subcategories.SecureDelete
        ];
        foreach (var sub in required)
            Assert.True(HelperLog.Taxonomy.IsSubcategoryRegistered(HelperLog.Category, sub), sub);
    }

    [Fact]
    public void Json_subcategories_are_registered()
    {
        string[] required =
        [
            HelperLog.Subcategories.Probe,
            HelperLog.Subcategories.Identity,
            HelperLog.Subcategories.Guard,
            HelperLog.Subcategories.Session,
            HelperLog.Subcategories.Document,
            HelperLog.Subcategories.Query,
            HelperLog.Subcategories.Snapshot,
            HelperLog.Subcategories.Diff,
            HelperLog.Subcategories.Commit,
            HelperLog.Subcategories.Save,
            HelperLog.Subcategories.Jsonl
        ];
        foreach (var sub in required)
            Assert.True(HelperLog.Taxonomy.IsSubcategoryRegistered(HelperLog.Category, sub), sub);
    }

    private static string Probe(string appId) => appId switch
    {
        HelperLog.AppIds.Core => HelperGuard.Probe(),
        HelperLog.AppIds.ClosedXml => WorkbookHelper.Probe(),
        HelperLog.AppIds.Encryption => EncryptionHelper.Probe(),
        HelperLog.AppIds.Hashing => HashingHelper.Probe(),
        HelperLog.AppIds.WinReg => RegistryHelper.Probe(),
        HelperLog.AppIds.Json => JsonHelper.Probe(),
        HelperLog.AppIds.Xml => XmlHelper.Probe(),
        HelperLog.AppIds.FileIo => FileIoHelper.Probe(),
        HelperLog.AppIds.Processes => ProcessHelper.Probe(),
        HelperLog.AppIds.Services => ServiceHelper.Probe(),
        HelperLog.AppIds.Analytics => AnalyticsHelper.Probe(),
        HelperLog.AppIds.Network => NetworkHelper.Probe(),
        HelperLog.AppIds.Csv => CsvHelper.Probe(),
        HelperLog.AppIds.Kql => KqlHelper.Probe(),
        _ => throw new ArgumentOutOfRangeException(nameof(appId), appId, "Unknown helper APPID.")
    };
}

[CollectionDefinition("Logger", DisableParallelization = true)]
public sealed class LoggerCollection;
