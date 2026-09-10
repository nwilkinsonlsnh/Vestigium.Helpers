using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Vestigium.Helpers.Json;
using Vestigium.Logging;

namespace Vestigium.Helpers.Tests;

[Collection("Logger")]
public sealed class JsonCoverageTests
{
    public JsonCoverageTests()
    {
        HelperLog.Shutdown();
    }

    [Fact]
    public void ReadOptions_default_max_depth_is_64()
        => Assert.Equal(64, new JsonReadOptions().MaxDepth);

    [Fact]
    public void FromJson_rejects_max_depth_below_one()
    {
        var zero = Assert.Throws<ArgumentOutOfRangeException>(
            () => JsonHelper.FromJson<ProbeDto>("{\"timeoutSeconds\":1}", new JsonReadOptions { MaxDepth = 0 }));
        Assert.Equal("options", zero.ParamName);
        Assert.Contains("MaxDepth", zero.Message, StringComparison.Ordinal);

        var negative = Assert.Throws<ArgumentOutOfRangeException>(
            () => JsonHelper.FromJson<ProbeDto>("{\"timeoutSeconds\":1}", new JsonReadOptions { MaxDepth = -1 }));
        Assert.Equal("options", negative.ParamName);
    }

    [Fact]
    public void FromJson_custom_max_depth_rejects_a_deeper_payload()
    {
        const string nested = """{"a":{"b":{"c":1}}}""";
        Assert.ThrowsAny<JsonException>(() =>
            JsonHelper.FromJson<JsonElement>(nested, new JsonReadOptions { MaxDepth = 2 }));
        var custom = JsonCodec.Read(new JsonReadOptions { MaxDepth = 8 });
        Assert.NotSame(JsonCodec.Read(null), custom);
        Assert.Equal(8, custom.MaxDepth);
        Assert.Same(JsonCodec.Read(null), JsonCodec.Read(new JsonReadOptions { MaxDepth = 64 }));
        Assert.Equal(64, JsonCodec.DefaultMaxDepth);
    }

    [Fact]
    public void Parse_rejects_json_null_and_accepts_a_primitive_root()
    {
        var nullEx = Assert.Throws<JsonException>(() => JsonHelper.Parse("null"));
        Assert.Contains("JSON null", nullEx.Message, StringComparison.Ordinal);
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes("null"));
        Assert.Throws<JsonException>(() => JsonHelper.Parse(stream));

        var number = JsonHelper.Parse("15");
        Assert.Equal(15, number.GetValue<int>());
        var array = JsonHelper.Parse("[1,2]");
        Assert.Equal(2, array.AsArray().Count);
    }

    [Fact]
    public void Parse_rejects_a_write_only_stream()
    {
        var path = Path.Combine(Path.GetTempPath(), "VestigiumJsonTests", Guid.NewGuid().ToString("N"), "w.json");
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        using var write = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None);
        var ex = Assert.Throws<ArgumentException>(() => JsonHelper.Parse(write));
        Assert.Equal("stream", ex.ParamName);
    }

    [Fact]
    public void ToJson_and_WriteFile_reject_null_values()
    {
        Assert.Throws<ArgumentNullException>(() => JsonHelper.ToJson<string>(null!));
        var path = Path.Combine(Path.GetTempPath(), "VestigiumJsonTests", Guid.NewGuid().ToString("N"), "n.json");
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        Assert.Throws<ArgumentNullException>(() => JsonHelper.WriteFile<object>(path, null!));
    }

    [Fact]
    public void WriteFile_non_atomic_overwrite_round_trips()
    {
        var dir = Path.Combine(Path.GetTempPath(), "VestigiumJsonTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        var path = Path.Combine(dir, "compact.json");
        JsonHelper.WriteFile(path, new ProbeDto { TimeoutSeconds = 1, Level = "a" }, new JsonWriteOptions
        {
            WriteIndented = false,
            Collision = JsonCollision.Overwrite,
            AtomicWrite = false
        });
        using var doc = JsonHelper.Open(path);
        Assert.Equal(1, doc.Get<int>("timeoutSeconds"));
        Assert.DoesNotContain('\n', File.ReadAllText(path).TrimEnd());
    }

    [Fact]
    public void Open_rejects_json_null_and_truncated_files()
    {
        var dir = Path.Combine(Path.GetTempPath(), "VestigiumJsonTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        var nullPath = Path.Combine(dir, "null.json");
        File.WriteAllText(nullPath, "null");
        Assert.Throws<JsonException>(() => JsonHelper.Open(nullPath));
        var truncated = Path.Combine(dir, "bad.json");
        File.WriteAllText(truncated, "{\"a\":");
        Assert.ThrowsAny<JsonException>(() => JsonHelper.Open(truncated));
    }

    [Fact]
    public void NewExportPath_rejects_dot_stems()
    {
        var root = Path.Combine(Path.GetTempPath(), "VestigiumJsonExports", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        JsonTestHooks.ExportRoot = root;
        try
        {
            Assert.Throws<ArgumentException>(() => JsonHelper.NewExportPath("."));
            Assert.Throws<ArgumentException>(() => JsonHelper.NewExportPath(".."));
            var jsonl = JsonHelper.NewExportPath("payload-records", JsonDocumentKind.Jsonl);
            Assert.EndsWith(".jsonl", jsonl);
        }
        finally
        {
            JsonTestHooks.ExportRoot = null;
        }
    }

    [Fact]
    public void Path_parse_empty_is_root_and_blank_still_throws()
    {
        Assert.Same(JsonPath.Root, JsonPath.Parse(""));
        Assert.Empty(JsonPath.Root.Segments);
        Assert.Throws<ArgumentException>(() => JsonPath.Parse("   "));
        Assert.Throws<ArgumentNullException>(() => JsonPath.Parse(null));
    }

    [Fact]
    public void Path_parse_rejects_escapes_and_trailing_dots()
    {
        var tilde = Assert.Throws<ArgumentException>(() => JsonPath.Parse("/a~2"));
        Assert.Contains("invalid '~' escape", tilde.Message, StringComparison.Ordinal);
        var dangling = Assert.Throws<ArgumentException>(() => JsonPath.Parse("/a~"));
        Assert.Contains("invalid '~' escape", dangling.Message, StringComparison.Ordinal);
        var trailing = Assert.Throws<ArgumentException>(() => JsonPath.Parse("network."));
        Assert.Contains("expected a name", trailing.Message, StringComparison.Ordinal);
        var leadingDot = Assert.Throws<ArgumentException>(() => JsonPath.Parse(".foo"));
        Assert.Contains("expected a name", leadingDot.Message, StringComparison.Ordinal);
        var unclosed = Assert.Throws<ArgumentException>(() => JsonPath.Parse("records[0"));
        Assert.Contains("expected ']'", unclosed.Message, StringComparison.Ordinal);
        var huge = Assert.Throws<ArgumentException>(() => JsonPath.Parse("records[9999999999999999999]"));
        Assert.Contains("array index is out of range", huge.Message, StringComparison.Ordinal);
        var emptyIndex = Assert.Throws<ArgumentException>(() => JsonPath.Parse("records[]"));
        Assert.Contains("expected an array index", emptyIndex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Pointer_empty_token_and_slash_root_are_legal()
    {
        using var doc = JsonHelper.Create();
        doc.Set("/", 7);
        Assert.Equal(7, doc.Get<int>("/"));
        var rootPointer = JsonPath.Parse("/");
        Assert.Single(rootPointer.Segments);
        Assert.Equal("", rootPointer.Segments[0].Token);
    }

    [Fact]
    public void Assign_root_null_is_rejected_and_root_replace_works()
    {
        using var doc = JsonHelper.Create();
        var nullRoot = Assert.Throws<InvalidOperationException>(() => doc.Set("", null));
        Assert.Contains("JSON null is not a document root", nullRoot.Message, StringComparison.Ordinal);

        doc.Set("", JsonHelper.Parse("""{"appId":"PingIQ"}"""));
        Assert.Equal("PingIQ", doc.Get<string>("appId"));

        doc.Set("", 15);
        Assert.Equal(15, doc.Get<int>(""));
        Assert.Throws<InvalidOperationException>(() => doc.Set("a", 1));
    }

    [Fact]
    public void SetChild_rejects_missing_array_index_and_object_index()
    {
        using var doc = JsonHelper.Create();
        doc.Set("records", JsonHelper.Parse("""[{"code":"alpha"}]"""));
        var missing = Assert.Throws<InvalidOperationException>(() => doc.Set("records[1]", "beta"));
        Assert.Contains("array index is missing", missing.Message, StringComparison.Ordinal);

        var throughValue = Assert.Throws<InvalidOperationException>(() => doc.Set("records[0].code.extra", 1));
        Assert.Contains("cannot create through a primitive", throughValue.Message, StringComparison.Ordinal);

        using var objects = JsonHelper.Create();
        objects.Set("network", JsonHelper.Parse("""{"timeoutSeconds":15}"""));
        var indexed = Assert.Throws<InvalidOperationException>(() => objects.Set("network[0]", 1));
        Assert.Contains("cannot index an object", indexed.Message, StringComparison.Ordinal);

        var nestedIndex = Assert.Throws<InvalidOperationException>(() => objects.Set("network[0].x", 1));
        Assert.Contains("cannot index an object", nestedIndex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void EnsureChild_rejects_array_parents_and_primitives()
    {
        using var doc = JsonHelper.Create();
        var missingParent = Assert.Throws<InvalidOperationException>(() => doc.Set("records[0].code", "ok"));
        Assert.Contains("cannot create array parents", missingParent.Message, StringComparison.Ordinal);

        doc.Set("records", JsonHelper.Parse("[1]"));
        var throughPrimitive = Assert.Throws<InvalidOperationException>(() => doc.Set("records[0].x", 2));
        Assert.Contains("cannot create through a primitive", throughPrimitive.Message, StringComparison.Ordinal);

        var oobParent = Assert.Throws<InvalidOperationException>(() => doc.Set("records[3].x", 2));
        Assert.Contains("cannot create array parents", oobParent.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Get_type_mismatch_is_logged_as_failed()
    {
        var dir = Path.Combine(Path.GetTempPath(), "VestigiumJsonTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        HelperLog.InitializeHost(HelperLog.AppIds.Json, cfg => cfg.LogDirectory = dir);
        try
        {
            using var doc = JsonHelper.Create();
            doc.Set("level", "Information");
            var ex = Assert.Throws<InvalidOperationException>(() => doc.Get<int>("level"));
            Assert.Contains("int", ex.Message, StringComparison.OrdinalIgnoreCase);
            Assert.Contains(HelperLog.RecentJsonLines, l =>
                l.Contains("\"STATUS\":\"Failed\"") && l.Contains("path type mismatch") && l.Contains("level"));
        }
        finally
        {
            HelperLog.Shutdown();
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public void Diff_arrays_truncate_high_index_first_then_extend()
    {
        using var doc = JsonHelper.Create();
        doc.Set("items", JsonHelper.Parse("[1,2,3]"));
        doc.Commit();
        doc.Snapshot();
        doc.Set("items", JsonHelper.Parse("[1]"));
        var truncated = doc.Diff();
        Assert.Equal(2, truncated.Count);
        Assert.Equal("remove", truncated.Operations[0].Op);
        Assert.Equal("/items/2", truncated.Operations[0].Path);
        Assert.Null(truncated.Operations[0].Value);
        Assert.Equal("remove", truncated.Operations[1].Op);
        Assert.Equal("/items/1", truncated.Operations[1].Path);
        var json = truncated.ToJsonArray();
        Assert.False(((JsonObject)json[0]!).ContainsKey("value"));

        doc.Revert();
        doc.Snapshot();
        doc.Set("items", JsonHelper.Parse("[1,2,3,4,5]"));
        var extended = doc.Diff();
        Assert.Equal(2, extended.Count);
        Assert.Equal("add", extended.Operations[0].Op);
        Assert.Equal("/items/3", extended.Operations[0].Path);
        Assert.Equal(4, extended.Operations[0].Value!.GetValue<int>());
        Assert.Equal("add", extended.Operations[1].Op);
        Assert.Equal("/items/4", extended.Operations[1].Path);
        Assert.Equal(5, extended.Operations[1].Value!.GetValue<int>());
    }

    [Fact]
    public void Diff_arrays_replace_shared_slots_and_walk_nested_arrays()
    {
        using var doc = JsonHelper.Create();
        doc.Set("items", JsonHelper.Parse("""[1,[2,3],{"k":1}]"""));
        doc.Commit();
        doc.Snapshot();
        doc.Set("items", JsonHelper.Parse("""[9,[2,4,5],{"k":1}]"""));
        var patch = doc.Diff();
        Assert.Contains(patch.Operations, op => op.Op == "replace" && op.Path == "/items/0");
        Assert.Contains(patch.Operations, op => op.Op == "replace" && op.Path == "/items/1/1");
        Assert.Contains(patch.Operations, op => op.Op == "add" && op.Path == "/items/1/2");
        Assert.DoesNotContain(patch.Operations, op => op.Path == "/items/2");
    }

    [Fact]
    public void Diff_objects_remove_add_and_escape_slash_tilde_keys()
    {
        using var doc = JsonHelper.Create();
        doc.Set("/a~1b", 1);
        doc.Set("/m~0n", 2);
        doc.Set("keep", 3);
        doc.Commit();
        doc.Snapshot();
        doc.Set("", JsonHelper.Parse("""{"m~n":2,"keep":3,"z":4}"""));
        var patch = doc.Diff();
        Assert.Contains(patch.Operations, op => op.Op == "remove" && op.Path == "/a~1b");
        Assert.Contains(patch.Operations, op => op.Op == "add" && op.Path == "/z");
        Assert.DoesNotContain(patch.Operations, op => op.Path == "/m~0n");
        Assert.DoesNotContain(patch.Operations, op => op.Path == "/keep");
    }

    [Fact]
    public void Diff_replaces_when_node_kinds_differ()
    {
        using var doc = JsonHelper.Create();
        doc.Set("payload", JsonHelper.Parse("""{"a":1}"""));
        doc.Commit();
        doc.Snapshot();
        doc.Set("payload", JsonHelper.Parse("[1,2]"));
        var patch = doc.Diff();
        Assert.Equal(1, patch.Count);
        Assert.Equal("replace", patch.Operations[0].Op);
        Assert.Equal("/payload", patch.Operations[0].Path);
        Assert.IsType<JsonArray>(patch.Operations[0].Value);
    }

    [Fact]
    public void Set_accepts_json_element_and_json_node()
    {
        using var doc = JsonHelper.Create();
        using var parsed = JsonDocument.Parse("""{"timeoutSeconds":15}""");
        doc.Set("network", parsed.RootElement);
        Assert.Equal(15, doc.Get<int>("network.timeoutSeconds"));
        doc.Set("copy", JsonHelper.Parse("""{"level":"Debug"}"""));
        Assert.Equal("Debug", doc.Get<string>("copy.level"));
    }

    [Fact]
    public void JsonPatch_compare_is_empty_when_nodes_match()
    {
        var node = JsonHelper.Parse("""{"a":[1,2]}""");
        var patch = JsonPatch.Compare(node, node.DeepClone());
        Assert.Equal(0, patch.Count);
        Assert.Equal("0 operation(s)", patch.ToString());
    }

    [Fact]
    public void JsonPatch_compare_truncates_arrays_high_index_first()
    {
        var patch = JsonPatch.Compare(JsonNode.Parse("[1,2,3,4]"), JsonNode.Parse("[1,9]"));
        Assert.Equal(new[] { "replace", "remove", "remove" }, patch.Operations.Select(o => o.Op));
        Assert.Equal(new[] { "/1", "/3", "/2" }, patch.Operations.Select(o => o.Path));
        Assert.Null(patch.ToJsonArray()[1]!["value"]);
        Assert.Equal(9, patch.Operations[0].Value!.GetValue<int>());
    }

    [Fact]
    public void Primitive_root_cannot_set_a_member()
    {
        var dir = Path.Combine(Path.GetTempPath(), "VestigiumJsonTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        var path = Path.Combine(dir, "n.json");
        File.WriteAllText(path, "42");
        using var doc = JsonHelper.Open(path);
        Assert.Equal(42, doc.Get<int>(""));
        var setMember = Assert.Throws<InvalidOperationException>(() => doc.Set("a", 1));
        Assert.Contains("cannot set a member on a primitive", setMember.Message, StringComparison.Ordinal);
        var through = Assert.Throws<InvalidOperationException>(() => doc.Set("a.b", 1));
        Assert.Contains("cannot create through a primitive", through.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Jsonl_record_index_bounds_and_json_session_rejects_record()
    {
        var dir = Path.Combine(Path.GetTempPath(), "VestigiumJsonTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        var path = Path.Combine(dir, "rows.jsonl");
        File.WriteAllText(path, """{"code":"a"}""" + "\n");
        using var jsonl = JsonHelper.OpenJsonl(path);
        Assert.Throws<ArgumentOutOfRangeException>(() => jsonl.Record(-1));
        Assert.Null(jsonl.Record(99));
        using var json = JsonHelper.Create();
        Assert.Throws<InvalidOperationException>(() => json.Record(0));
        Assert.Throws<InvalidOperationException>(() => _ = json.RecordCount);
    }

    [Fact]
    public void Index_on_object_root_is_rejected()
    {
        using var doc = JsonHelper.Create();
        var indexed = Assert.Throws<InvalidOperationException>(() => doc.Set("[0]", 1));
        Assert.Contains("cannot index an object", indexed.Message, StringComparison.Ordinal);
        var nested = Assert.Throws<InvalidOperationException>(() => doc.Set("[0].x", 1));
        Assert.Contains("cannot index an object", nested.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void DefaultExportDirectory_without_hook_stays_under_exports_json()
    {
        JsonTestHooks.ExportRoot = null;
        var dir = JsonHelper.DefaultExportDirectory();
        Assert.Contains(Path.Combine("Vestigium", "Exports", "Json"), dir);
    }

    private sealed class ProbeDto
    {
        public int TimeoutSeconds { get; set; }
        public string Level { get; set; } = "";
    }

    [Fact]
    public void Get_json_node_null_and_save_without_path()
    {
        using var doc = JsonHelper.Create();
        doc.Set("level", null);
        Assert.Null(doc.Get<string>("level"));
        Assert.True(doc.TryGet<string>("level", out var asString) && asString is null);
        Assert.Throws<InvalidOperationException>(() => doc.Get<int>("level"));
        Assert.Null(doc.Get<int?>("level"));

        doc.Set("network", JsonHelper.Parse("""{"timeoutSeconds":15}"""));
        var node = doc.Get<JsonNode>("network");
        Assert.IsType<JsonObject>(node);
        Assert.Equal(15, doc.Get<JsonObject>("network")!["timeoutSeconds"]!.GetValue<int>());
        Assert.Throws<InvalidOperationException>(() => doc.Get<JsonArray>("network"));

        doc.Set("items", JsonHelper.Parse("[1,2]"));
        doc.Set("items[0]", null);

        var root = Path.Combine(Path.GetTempPath(), "VestigiumJsonExports", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        JsonTestHooks.ExportRoot = root;
        try
        {
            doc.Commit();
            var saved = doc.Save();
            Assert.True(File.Exists(saved));
            Assert.Equal(saved, doc.Path);

        using var working = JsonHelper.Create(options: new JsonSessionOptions
            {
                Collision = JsonCollision.Overwrite,
                AtomicWrite = false
            });
            working.Set("n", 1);
            var workingRoot = Path.Combine(root, "working");
            Directory.CreateDirectory(workingRoot);
            JsonTestHooks.ExportRoot = workingRoot;
            var w = working.SaveWorking();
            Assert.True(File.Exists(w));
        }
        finally
        {
            JsonTestHooks.ExportRoot = null;
        }
    }

    [Fact]
    public void Jsonl_root_replaced_with_object_rejects_records()
    {
        var dir = Path.Combine(Path.GetTempPath(), "VestigiumJsonTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        var path = Path.Combine(dir, "rows.jsonl");
        using var jsonl = JsonHelper.Create(path);
        Assert.Equal(JsonDocumentKind.Jsonl, jsonl.Kind);
        jsonl.AppendRecord(JsonNode.Parse("""{"a":1}""")!);
        Assert.Equal(1, jsonl.RecordCount);
        jsonl.Set("", JsonHelper.Parse("""{"not":"array"}"""));
        Assert.Throws<InvalidOperationException>(() => jsonl.RecordCount);
        Assert.Throws<InvalidOperationException>(() => jsonl.Record(0));
        Assert.Throws<InvalidOperationException>(() => jsonl.AppendRecord(JsonNode.Parse("{}")!));
        jsonl.Commit();
        Assert.Throws<InvalidOperationException>(() => jsonl.Save());
    }

    [Fact]
    public void KindFromPath_SamePath_and_dot_stems()
    {
        Assert.Equal(JsonDocumentKind.Json, JsonIO.KindFromPath(null));
        Assert.Equal(JsonDocumentKind.Json, JsonIO.KindFromPath(""));
        Assert.Equal(JsonDocumentKind.Json, JsonIO.KindFromPath("a.json"));
        Assert.Equal(JsonDocumentKind.Json, JsonIO.KindFromPath("a.jsonl.bak"));
        Assert.Equal(JsonDocumentKind.Jsonl, JsonIO.KindFromPath("a.JSONL"));
        Assert.False(JsonIO.SamePath(null, "a.json"));
        Assert.False(JsonIO.SamePath("  ", "a.json"));
        var full = Path.GetFullPath("x.json");
        Assert.True(JsonIO.SamePath(full, full));
        var root = Path.Combine(Path.GetTempPath(), "VestigiumJsonExports", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        Assert.Throws<ArgumentException>(() => JsonIO.ResolveExportFile(root, "  ", JsonDocumentKind.Json));
        Assert.Throws<ArgumentException>(() => JsonIO.ResolveExportFile(root, ".", JsonDocumentKind.Json));
        Assert.Throws<ArgumentException>(() => JsonIO.ResolveExportFile(root, "..", JsonDocumentKind.Jsonl));
        var named = JsonIO.ResolveExportFile(root, "payload", JsonDocumentKind.Json);
        Assert.EndsWith(".json", named);
    }

    [Fact]
    public void Dispose_twice_is_idempotent()
    {
        var doc = JsonHelper.Create();
        doc.Dispose();
        doc.Dispose();
        Assert.Throws<ObjectDisposedException>(() => doc.Set("a", 1));
    }

}
