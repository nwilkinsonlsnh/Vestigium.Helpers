using System.Text;
using Vestigium.Helpers.Json;

namespace Vestigium.Helpers.Tests;

[Collection("Logger")]
public sealed class JsonCompareTests : IDisposable
{
    private readonly string _root;

    public JsonCompareTests()
    {
        _root = Path.Combine(Path.GetTempPath(), "VestigiumJsonCompare", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_root);
        JsonTestHooks.ExportRoot = _root;
    }

    public void Dispose()
    {
        JsonTestHooks.ExportRoot = null;
        if (Directory.Exists(_root))
            Directory.Delete(_root, recursive: true);
    }

    [Fact]
    public void Node_compare_is_empty_when_trees_match()
    {
        var node = JsonHelper.Parse("""{"level":"Information"}""");
        var patch = JsonPatch.Compare(node, node.DeepClone());
        Assert.Equal(0, patch.Count);
    }

    [Fact]
    public void File_compare_one_replaced_member()
    {
        var left = Path.Combine(_root, "left.json");
        var right = Path.Combine(_root, "right.json");
        JsonHelper.WriteFile(left, new { Level = "Information" });
        JsonHelper.WriteFile(right, new { Level = "Debug" });
        var patch = JsonHelper.Compare(left, right);
        Assert.Equal(1, patch.Count);
        Assert.Equal("replace", patch.Operations[0].Op);
        Assert.Equal("/level", patch.Operations[0].Path);
    }

    [Fact]
    public void File_compare_same_payload_is_empty()
    {
        var left = Path.Combine(_root, "a.json");
        var right = Path.Combine(_root, "b.json");
        JsonHelper.WriteFile(left, new { N = 1 });
        JsonHelper.WriteFile(right, new { N = 1 });
        Assert.Equal(0, JsonHelper.Compare(left, right).Count);
    }

    [Fact]
    public void Mixed_kind_throws()
    {
        var json = Path.Combine(_root, "doc.json");
        var jsonl = Path.Combine(_root, "rows.jsonl");
        JsonHelper.WriteFile(json, new { N = 1 });
        File.WriteAllText(jsonl, "{\"n\":1}\n", new UTF8Encoding(false));
        var ex = Assert.Throws<ArgumentException>(() => JsonHelper.Compare(json, jsonl));
        Assert.Contains("same document kind", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Jsonl_files_compare_as_arrays()
    {
        var left = Path.Combine(_root, "left.jsonl");
        var right = Path.Combine(_root, "right.jsonl");
        File.WriteAllText(left, "{\"n\":1}\n{\"n\":2}\n", new UTF8Encoding(false));
        File.WriteAllText(right, "{\"n\":1}\n{\"n\":9}\n", new UTF8Encoding(false));
        var patch = JsonHelper.Compare(left, right);
        Assert.Equal(1, patch.Count);
        Assert.Equal("replace", patch.Operations[0].Op);
        Assert.Equal("/1/n", patch.Operations[0].Path);
    }
}
