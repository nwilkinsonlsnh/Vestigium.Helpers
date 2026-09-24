using System.Reflection;
using System.Text.Json;
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
    public void Compare_kind_mismatch_writes_diff_failed_not_guard()
    {
        var export = Path.Combine(Path.GetTempPath(), "VestigiumJsonPr07Events", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(export);
        JsonTestHooks.ExportRoot = export;
        try
        {
            Init();
            var jsonPath = Path.Combine(export, "left.json");
            var jsonlPath = Path.Combine(export, "right.jsonl");
            JsonHelper.WriteFile(jsonPath, new { N = 1 });
            File.WriteAllText(jsonlPath, "{\"n\":1}\n");
            Assert.Throws<ArgumentException>(() => JsonHelper.Compare(jsonPath, jsonlPath));

            VestigiumLogger.Flush();
            var log = VestigiumLogger.RecentJsonLines;
            Assert.Contains(log, line =>
                line.Contains("\"EVENTID\":13635") && line.Contains("\"SUBCATEGORY\":\"Diff\""));
            Assert.DoesNotContain(log, line =>
                line.Contains("\"SUBCATEGORY\":\"Diff\"") && line.Contains("\"EVENTID\":13620"));
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
        Assert.Equal(29, JsonCatalog.Rows.Length);
        Assert.All(JsonCatalog.Rows, row =>
        {
            Assert.InRange(row.EventId, JsonEvents.BlockStart, JsonEvents.BlockEnd);
            Assert.Equal(0, row.EventId % 5);
        });
        Assert.Equal(JsonEvents.ProbeEnter, JsonCatalog.Rows[0].EventId);
        Assert.Equal(JsonEvents.CommitFailed, JsonCatalog.Rows[^1].EventId);
        Assert.Contains(JsonCatalog.Rows, row => row.EventId == JsonEvents.SnapshotFailed && row.Name == "SnapshotFailed");
        Assert.Contains(JsonCatalog.Rows, row => row.EventId == JsonEvents.DiffFailed && row.Name == "DiffFailed");
        Assert.Contains(JsonCatalog.Rows, row => row.EventId == JsonEvents.CommitFailed && row.Name == "CommitFailed");
        Assert.Equal("Probe", JsonCatalog.Rows[0].Subcategory);
        Assert.DoesNotContain(JsonCatalog.Rows, row => row.Name is "OperationEnter" or "OperationComplete" or "OperationFailed");
    }

    [Fact]
    public void Catalog_file_matches_rows_and_event_constants()
    {
        var consts = typeof(JsonEvents)
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(f => f.FieldType == typeof(int) && f.Name is not "BlockStart" and not "BlockEnd")
            .Select(f => (Id: (int)f.GetValue(null)!, f.Name))
            .ToArray();
        Assert.Equal(JsonCatalog.Rows.Length, consts.Length);
        foreach (var (id, name) in consts)
            Assert.Contains(JsonCatalog.Rows, row => row.EventId == id && row.Name == name);

        var catalog = Path.Combine(
            Path.GetDirectoryName(typeof(JsonCatalog).Assembly.Location)!,
            "EventCatalog",
            "json.json");
        Assert.True(File.Exists(catalog), catalog);
        using var doc = JsonDocument.Parse(File.ReadAllText(catalog));
        var events = doc.RootElement.GetProperty("events");
        Assert.Equal(JsonCatalog.Rows.Length, events.GetArrayLength());
        var i = 0;
        foreach (var item in events.EnumerateArray())
        {
            var row = JsonCatalog.Rows[i++];
            Assert.Equal(row.EventId, item.GetProperty("eventId").GetInt32());
            Assert.Equal(row.Name, item.GetProperty("name").GetString());
            Assert.Equal(row.Subcategory, item.GetProperty("subcategory").GetString());
            Assert.Equal(row.Severity, item.GetProperty("severity").GetString());
        }
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
