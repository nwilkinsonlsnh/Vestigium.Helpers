using Vestigium.Helpers.Json;
using Vestigium.Logging;

namespace Vestigium.Helpers.Tests;

[Collection("Logger")]
public sealed class JsonLoggingTests
{
    public JsonLoggingTests()
    {
        VestigiumLogger.Shutdown();
    }

    private static string Init()
    {
        var dir = Path.Combine(Path.GetTempPath(), "VestigiumJsonLog", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        VestigiumLogger.Initialize(cfg =>
        {
            cfg.AppId = JsonCatalog.AppId;
            cfg.LogDirectory = dir;
            cfg.MinimumDiskLevel = VestigiumLogLevel.Debug;
            cfg.OperationsLogEnabled = false;
            JsonCatalog.Register(cfg);
        });
        return dir;
    }

    [Fact]
    public void Probe_writes_13505()
    {
        try
        {
            Init();
            Assert.Equal("Vestigium.Helpers.Json", JsonHelper.Probe());
            VestigiumLogger.Flush();
            Assert.Contains(VestigiumLogger.RecentJsonLines, line => line.Contains("\"EVENTID\":13505"));
            Assert.Contains(VestigiumLogger.RecentJsonLines, line => line.Contains("\"EVENTID\":13500"));
        }
        finally
        {
            VestigiumLogger.Shutdown();
        }
    }

    [Fact]
    public void Probe_without_host_does_not_throw()
    {
        Assert.Equal("Vestigium.Helpers.Json", JsonHelper.Probe());
    }

    [Fact]
    public void Session_save_commit_jsonl_use_distinct_event_ids()
    {
        var export = Path.Combine(Path.GetTempPath(), "VestigiumJsonPr05", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(export);
        JsonTestHooks.ExportRoot = export;
        try
        {
            Init();
            var jsonPath = Path.Combine(export, "settings.json");
            JsonHelper.WriteFile(jsonPath, new { Level = "Information" });
            using (var doc = JsonHelper.Open(jsonPath))
            {
                doc.Snapshot();
                doc.Set("level", "Debug");
                _ = doc.Diff();
                doc.Commit();
                doc.Save();
            }

            var jsonlPath = Path.Combine(export, "records.jsonl");
            File.WriteAllText(jsonlPath, "{\"n\":1}\n");
            using (var lines = JsonHelper.OpenJsonl(jsonlPath))
                Assert.Equal(1, lines.RecordCount);

            VestigiumLogger.Flush();
            var log = VestigiumLogger.RecentJsonLines;
            Assert.Contains(log, line => line.Contains("\"EVENTID\":13585") && line.Contains("\"SUBCATEGORY\":\"Save\""));
            Assert.Contains(log, line => line.Contains("\"EVENTID\":13590") && line.Contains("\"SUBCATEGORY\":\"Save\""));
            Assert.Contains(log, line => line.Contains("\"EVENTID\":13575") && line.Contains("\"SUBCATEGORY\":\"Commit\""));
            Assert.Contains(log, line => line.Contains("\"EVENTID\":13580") && line.Contains("\"SUBCATEGORY\":\"Commit\""));
            Assert.Contains(log, line => line.Contains("\"EVENTID\":13605") && line.Contains("\"SUBCATEGORY\":\"Jsonl\""));
            Assert.DoesNotContain(log, line =>
                line.Contains("\"SUBCATEGORY\":\"Save\"") && line.Contains("\"EVENTID\":13515"));
            Assert.DoesNotContain(log, line =>
                line.Contains("\"SUBCATEGORY\":\"Commit\"") && line.Contains("\"EVENTID\":13515"));
            Assert.DoesNotContain(log, line =>
                line.Contains("\"SUBCATEGORY\":\"Jsonl\"") && line.Contains("\"EVENTID\":13515"));
            Assert.All(log, line => Assert.DoesNotContain("\"EXCEPTION\":\"", line.Replace("\"EXCEPTION\":null", "")));
        }
        finally
        {
            JsonTestHooks.ExportRoot = null;
            VestigiumLogger.Shutdown();
            if (Directory.Exists(export))
                Directory.Delete(export, recursive: true);
        }
    }

    [Fact]
    public void Catalog_rows_cover_the_pr05_block_and_stay_in_range()
    {
        Assert.Equal(26, JsonCatalog.Rows.Length);
        Assert.All(JsonCatalog.Rows, row =>
        {
            Assert.InRange(row.EventId, JsonEvents.BlockStart, JsonEvents.BlockEnd);
            Assert.Equal(0, row.EventId % 5);
        });
        Assert.Equal(JsonEvents.ProbeEnter, JsonCatalog.Rows[0].EventId);
        Assert.Equal(JsonEvents.OperationWarning, JsonCatalog.Rows[^1].EventId);
        Assert.Equal("Probe", JsonCatalog.Rows[0].Subcategory);
        Assert.DoesNotContain(JsonCatalog.Rows, row => row.Name is "OperationEnter" or "OperationComplete" or "OperationFailed");
    }

    [Fact]
    public void Get_and_TryGet_do_not_write_query_failed()
    {
        var export = Path.Combine(Path.GetTempPath(), "VestigiumJsonPr06Get", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(export);
        JsonTestHooks.ExportRoot = export;
        try
        {
            Init();
            using var doc = JsonHelper.Create();
            doc.Set("level", "Information");
            Assert.Equal("Information", doc.Get<string>("level"));
            Assert.False(doc.TryGet<string>("missing", out _));
            Assert.Throws<KeyNotFoundException>(() => doc.Get<string>("missing"));
            Assert.Throws<InvalidOperationException>(() => doc.Get<int>("level"));

            VestigiumLogger.Flush();
            var log = VestigiumLogger.RecentJsonLines;
            Assert.DoesNotContain(log, line =>
                line.Contains("\"SUBCATEGORY\":\"Query\"") && line.Contains("\"STATUS\":\"Failed\""));
            Assert.DoesNotContain(log, line => line.Contains("path not found"));
            Assert.DoesNotContain(log, line => line.Contains("path type mismatch"));
            Assert.Contains(log, line =>
                line.Contains("\"SUBCATEGORY\":\"Query\"") && line.Contains("Set path=level"));
            Assert.All(log, line => Assert.DoesNotContain("\"EXCEPTION\":\"", line.Replace("\"EXCEPTION\":null", "")));
        }
        finally
        {
            JsonTestHooks.ExportRoot = null;
            VestigiumLogger.Shutdown();
            if (Directory.Exists(export))
                Directory.Delete(export, recursive: true);
        }
    }
}
