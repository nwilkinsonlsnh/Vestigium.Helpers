using System.Text.Json.Nodes;
using Vestigium.Helpers.Json;
using Vestigium.Logging;

namespace Vestigium.Helpers.Tests;

[Collection("Logger")]
public sealed class JsonSessionTests
{
    public JsonSessionTests()
    {
        HelperLog.Shutdown();
    }

    [Fact]
    public void Create_is_an_empty_object_in_memory()
    {
        using var doc = JsonHelper.Create();
        Assert.False(string.IsNullOrWhiteSpace(doc.SessionId));
        Assert.Null(doc.Path);
        Assert.Equal(JsonDocumentKind.Json, doc.Kind);
        Assert.False(doc.HasUncommittedWork);
        Assert.False(doc.HasUnsavedCommit);
        Assert.False(doc.TryGet<int>("network.timeoutSeconds", out _));
    }

    [Fact]
    public void Set_creates_object_parents_and_round_trips_pointer_and_dotted()
    {
        using var doc = JsonHelper.Create();
        doc.Set("network.timeoutSeconds", 15);
        Assert.True(doc.HasUncommittedWork);
        Assert.False(doc.HasUnsavedCommit);
        Assert.Equal(15, doc.Get<int>("network.timeoutSeconds"));
        Assert.Equal(15, doc.Get<int>("/network/timeoutSeconds"));
        Assert.True(doc.TryGet<int>("network.timeoutSeconds", out var value));
        Assert.Equal(15, value);
    }

    [Fact]
    public void Snapshot_set_diff_is_one_replace_then_revert_and_cancel()
    {
        using var doc = JsonHelper.Create();
        doc.Set("network.timeoutSeconds", 15);
        doc.Commit();
        Assert.False(doc.HasUncommittedWork);
        Assert.True(doc.HasUnsavedCommit);

        doc.Snapshot();
        doc.Set("/logging/level", "Information");
        doc.Set("network.timeoutSeconds", 30);
        var patch = doc.Diff();
        Assert.Equal(2, patch.Count);
        Assert.Contains(patch.Operations, op => op.Op == "replace" && op.Path == "/network/timeoutSeconds");
        Assert.Equal(30, patch.Operations.Single(op => op.Path == "/network/timeoutSeconds").Value!.GetValue<int>());
        Assert.Contains(patch.Operations, op => op.Op == "add" && op.Path == "/logging");

        var replaceOnly = JsonHelper.Create();
        replaceOnly.Set("network.timeoutSeconds", 15);
        replaceOnly.Commit();
        replaceOnly.Snapshot();
        replaceOnly.Set("network.timeoutSeconds", 30);
        var one = replaceOnly.Diff();
        Assert.Equal(1, one.Count);
        Assert.Equal("replace", one.Operations[0].Op);
        Assert.Equal("/network/timeoutSeconds", one.Operations[0].Path);
        Assert.Equal(30, one.Operations[0].Value!.GetValue<int>());

        replaceOnly.Revert();
        Assert.Equal(15, replaceOnly.Get<int>("network.timeoutSeconds"));
        Assert.False(replaceOnly.HasUncommittedWork);

        replaceOnly.Set("network.timeoutSeconds", 99);
        replaceOnly.Cancel();
        Assert.Equal(15, replaceOnly.Get<int>("network.timeoutSeconds"));
        Assert.False(replaceOnly.HasUncommittedWork);
        Assert.True(replaceOnly.HasUnsavedCommit);
    }

    [Fact]
    public void Diff_without_snapshot_is_against_committed()
    {
        using var doc = JsonHelper.Create();
        doc.Set("/appId", "PingIQ");
        var patch = doc.Diff();
        Assert.Equal(1, patch.Count);
        Assert.Equal("add", patch.Operations[0].Op);
        Assert.Equal("/appId", patch.Operations[0].Path);
        Assert.Equal("PingIQ", patch.Operations[0].Value!.GetValue<string>());
        doc.Cancel();
        Assert.False(doc.TryGet<string>("appId", out _));
        Assert.Equal(0, doc.Diff().Count);
    }

    [Fact]
    public void Set_through_a_primitive_or_missing_array_fails()
    {
        using var doc = JsonHelper.Create();
        doc.Set("network", 15);
        Assert.Throws<InvalidOperationException>(() => doc.Set("network.timeoutSeconds", 1));
        Assert.Throws<InvalidOperationException>(() => doc.Set("records[0].code", "ok"));
        Assert.Throws<ArgumentException>(() => doc.Set("records[*].code", "ok"));
    }

    [Fact]
    public void Missing_get_is_not_found()
    {
        using var doc = JsonHelper.Create();
        Assert.False(doc.TryGet<int>("nope", out _));
        Assert.Throws<KeyNotFoundException>(() => doc.Get<int>("nope"));
    }

    [Fact]
    public void Get_is_quiet_and_failed_get_is_logged()
    {
        var dir = Path.Combine(Path.GetTempPath(), "VestigiumJsonTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        HelperLog.InitializeHost(HelperLog.AppIds.Json, cfg => cfg.LogDirectory = dir);
        try
        {
            using var doc = JsonHelper.Create();
            doc.Set("network.timeoutSeconds", 15);
            var before = HelperLog.RecentJsonLines.Count;
            Assert.Equal(15, doc.Get<int>("network.timeoutSeconds"));
            Assert.True(doc.TryGet<int>("network.timeoutSeconds", out _));
            Assert.Equal(before, HelperLog.RecentJsonLines.Count);

            Assert.Throws<KeyNotFoundException>(() => doc.Get<int>("missing"));
            var lines = HelperLog.RecentJsonLines;
            Assert.Contains(lines, l => l.Contains("\"STATUS\":\"Failed\"") && l.Contains("path not found") && l.Contains("missing"));
            Assert.Contains(lines, l => l.Contains("\"SUBCATEGORY\":\"Query\""));
            Assert.DoesNotContain(lines, l => l.Contains("enter Get"));
        }
        finally
        {
            HelperLog.Shutdown();
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public void Array_member_set_requires_an_existing_object_record()
    {
        using var doc = JsonHelper.Create();
        doc.Set("records", JsonHelper.Parse("""[{"code":"alpha"}]"""));
        doc.Set("records[0].code", "beta");
        Assert.Equal("beta", doc.Get<string>("records[0].code"));
        Assert.Equal("beta", doc.Get<string>("/records/0/code"));
    }

    [Fact]
    public void Dispose_then_set_throws()
    {
        var doc = JsonHelper.Create();
        doc.Dispose();
        Assert.Throws<ObjectDisposedException>(() => doc.Set("a", 1));
    }

    [Fact]
    public void Session_logs_paths_and_op_counts_never_bodies()
    {
        var dir = Path.Combine(Path.GetTempPath(), "VestigiumJsonTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        HelperLog.InitializeHost(HelperLog.AppIds.Json, cfg => cfg.LogDirectory = dir);
        try
        {
            const string secret = "hunter2-session-must-not-log";
            using var doc = JsonHelper.Create();
            doc.Snapshot();
            doc.Set("level", secret);
            var patch = doc.Diff();
            Assert.Equal(1, patch.Count);
            doc.Commit();
            doc.Cancel();

            var lines = HelperLog.RecentJsonLines;
            Assert.DoesNotContain(lines, l => l.Contains(secret));
            Assert.Contains(lines, l => l.Contains("Set") && l.Contains("path=level"));
            Assert.Contains(lines, l => l.Contains("\"SUBCATEGORY\":\"Diff\"") && l.Contains("ops=1"));
            Assert.Contains(lines, l => l.Contains("\"SUBCATEGORY\":\"Snapshot\""));
            Assert.Contains(lines, l => l.Contains("\"SUBCATEGORY\":\"Commit\""));
            Assert.StartsWith(Path.GetTempPath().TrimEnd(Path.DirectorySeparatorChar), dir);
            Assert.DoesNotContain(dir, Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory)));
            Assert.All(lines, line => Assert.DoesNotContain("\"EXCEPTION\":\"", line.Replace("\"EXCEPTION\":null", "")));
        }
        finally
        {
            HelperLog.Shutdown();
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public void Patch_tostring_does_not_leak_values()
    {
        using var doc = JsonHelper.Create();
        doc.Set("level", "hunter2-patch-tostring");
        var patch = doc.Diff();
        Assert.Equal("1 operation(s)", patch.ToString());
        Assert.DoesNotContain("hunter2-patch-tostring", patch.ToString());
        var json = patch.ToJsonArray();
        Assert.Equal("add", json[0]!["op"]!.GetValue<string>());
        Assert.Equal("/level", json[0]!["path"]!.GetValue<string>());
    }
}
