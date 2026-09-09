using System.Text;
using Vestigium.Helpers.Json;
using Vestigium.Logging;

namespace Vestigium.Helpers.Tests;

[Collection("Logger")]
public sealed class JsonFileTests : IDisposable
{
    private readonly string _root;

    public JsonFileTests()
    {
        HelperLog.Shutdown();
        _root = Path.Combine(Path.GetTempPath(), "VestigiumJsonExports", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_root);
        JsonTestHooks.ExportRoot = _root;
    }

    public void Dispose()
    {
        JsonTestHooks.ExportRoot = null;
        HelperLog.Shutdown();
        if (Directory.Exists(_root))
            Directory.Delete(_root, recursive: true);
    }

    [Fact]
    public void Export_root_is_injected_temp_not_desktop()
    {
        var dir = JsonHelper.DefaultExportDirectory();
        Assert.Equal(Path.GetFullPath(_root), dir);
        Assert.StartsWith(Path.GetTempPath().TrimEnd(Path.DirectorySeparatorChar), dir);
        var desktop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
        if (!string.IsNullOrWhiteSpace(desktop))
            Assert.DoesNotContain(Path.GetFullPath(desktop), dir);
        var path = JsonHelper.NewExportPath("probe-settings");
        Assert.Equal(Path.Combine(dir, "probe-settings.json"), path);
    }

    [Fact]
    public void Probe_does_not_write_the_export_folder()
    {
        JsonHelper.Probe();
        Assert.Empty(Directory.GetFiles(_root, "*", SearchOption.AllDirectories));
    }

    [Fact]
    public void WriteFile_then_Open_round_trips()
    {
        var path = Path.Combine(_root, "settings.json");
        JsonHelper.WriteFile(path, new { Network = new { TimeoutSeconds = 15 }, Level = "Information" });
        using var doc = JsonHelper.Open(path);
        Assert.Equal(path, doc.Path);
        Assert.Equal(15, doc.Get<int>("network.timeoutSeconds"));
        Assert.Equal("Information", doc.Get<string>("level"));
        Assert.False(doc.HasUncommittedWork);
        Assert.False(doc.HasUnsavedCommit);
        var bytes = File.ReadAllBytes(path);
        Assert.False(bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF);
        Assert.Contains('\n', Encoding.UTF8.GetString(bytes));
    }

    [Fact]
    public void Commit_then_Save_persists_Save_without_Commit_does_not()
    {
        var path = Path.Combine(_root, "commit.json");
        using (var doc = JsonHelper.Create(path))
        {
            doc.Set("network.timeoutSeconds", 15);
            doc.Save();
        }

        using (var opened = JsonHelper.Open(path))
            Assert.False(opened.TryGet<int>("network.timeoutSeconds", out _));

        using (var doc = JsonHelper.Open(path))
        {
            doc.Set("network.timeoutSeconds", 15);
            doc.Commit();
            doc.Save();
            Assert.False(doc.HasUnsavedCommit);
        }

        using (var opened = JsonHelper.Open(path))
            Assert.Equal(15, opened.Get<int>("network.timeoutSeconds"));
    }

    [Fact]
    public void SaveWorking_writes_without_commit_Cancel_leaves_disk()
    {
        var path = Path.Combine(_root, "working.json");
        JsonHelper.WriteFile(path, new { Level = "Information" });
        using (var doc = JsonHelper.Open(path))
        {
            doc.Snapshot();
            doc.Set("level", "Debug");
            doc.SaveWorking();
        }

        using (var opened = JsonHelper.Open(path))
            Assert.Equal("Debug", opened.Get<string>("level"));

        using (var doc = JsonHelper.Open(path))
        {
            doc.Set("level", "Error");
            doc.Cancel();
            doc.Dispose();
        }

        using (var opened = JsonHelper.Open(path))
            Assert.Equal("Debug", opened.Get<string>("level"));
    }

    [Fact]
    public void SaveAs_existing_dest_fails_until_overwrite()
    {
        var original = Path.Combine(_root, "original.json");
        var dest = Path.Combine(_root, "dest.json");
        JsonHelper.WriteFile(original, new { Code = "keep" });
        JsonHelper.WriteFile(dest, new { Code = "old" });
        using var doc = JsonHelper.Open(original);
        doc.Set("code", "new");
        doc.Commit();
        var ex = Assert.Throws<IOException>(() => doc.SaveAs(dest));
        Assert.Contains(dest, ex.Message);
        using (var stayed = JsonHelper.Open(dest))
            Assert.Equal("old", stayed.Get<string>("code"));

        doc.SaveAs(dest, JsonCollision.Overwrite);
        using var replaced = JsonHelper.Open(dest);
        Assert.Equal("new", replaced.Get<string>("code"));
    }

    [Fact]
    public void WriteFile_existing_dest_fails_until_overwrite()
    {
        var path = Path.Combine(_root, "once.json");
        JsonHelper.WriteFile(path, new { N = 1 });
        Assert.Throws<IOException>(() => JsonHelper.WriteFile(path, new { N = 2 }));
        using (var stayed = JsonHelper.Open(path))
            Assert.Equal(1, stayed.Get<int>("n"));
        JsonHelper.WriteFile(path, new { N = 2 }, new JsonWriteOptions { Collision = JsonCollision.Overwrite });
        using var replaced = JsonHelper.Open(path);
        Assert.Equal(2, replaced.Get<int>("n"));
    }

    [Fact]
    public void Atomic_save_replaces_with_complete_file_and_leaves_no_tmp()
    {
        var path = Path.Combine(_root, "atomic.json");
        JsonHelper.WriteFile(path, new { N = 1 });
        using (var doc = JsonHelper.Open(path))
        {
            doc.Set("n", 2);
            doc.Commit();
            doc.Save();
        }

        using var opened = JsonHelper.Open(path);
        Assert.Equal(2, opened.Get<int>("n"));
        Assert.Empty(Directory.GetFiles(_root, "*.tmp", SearchOption.AllDirectories));
    }

    [Fact]
    public void OpenExport_reads_from_injected_folder()
    {
        JsonHelper.WriteFile(JsonHelper.NewExportPath("probe-settings"), new { AppId = "PingIQ" });
        using var doc = JsonHelper.OpenExport("probe-settings");
        Assert.Equal("PingIQ", doc.Get<string>("appId"));
        Assert.StartsWith(_root, doc.Path);
    }

    [Fact]
    public void Open_missing_file_throws()
        => Assert.Throws<FileNotFoundException>(() => JsonHelper.Open(Path.Combine(_root, "missing.json")));

    [Fact]
    public void Save_logs_path_and_bytes_never_bodies()
    {
        var logDir = Path.Combine(_root, "logs");
        Directory.CreateDirectory(logDir);
        HelperLog.InitializeHost(HelperLog.AppIds.Json, cfg => cfg.LogDirectory = logDir);
        const string secret = "hunter2-file-must-not-log";
        var path = Path.Combine(_root, "secret.json");
        JsonHelper.WriteFile(path, new { Level = secret });
        using var doc = JsonHelper.Open(path);
        doc.Set("level", "Information");
        doc.Commit();
        doc.Save();
        var lines = HelperLog.RecentJsonLines;
        Assert.DoesNotContain(lines, l => l.Contains(secret));
        Assert.Contains(lines, l => l.Contains("\"SUBCATEGORY\":\"Save\"") && l.Contains("bytes="));
        Assert.Contains(lines, l => l.Contains(path));
        Assert.All(lines, line => Assert.DoesNotContain("\"EXCEPTION\":\"", line.Replace("\"EXCEPTION\":null", "")));
    }
}
