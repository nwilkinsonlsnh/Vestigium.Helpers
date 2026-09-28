using System.Text.Json;
using Vestigium.Helpers.PerfMon.PageFile;

namespace Vestigium.Helpers.Tests;

public sealed class PerfMonPr03aTests
{
    [Fact]
    public void PR03a_001_pagefile_shard_is_stable_shape()
    {
        var jsonPath = Path.Combine(
            Path.GetDirectoryName(typeof(PageFilePerfCatalog).Assembly.Location)!,
            "EventCatalog",
            "pdh-categories.json");
        if (!File.Exists(jsonPath))
        {
            jsonPath = FindNamed("pdh-categories.json", "PerfMon.PageFile");
        }

        var text = File.ReadAllText(jsonPath);
        Assert.DoesNotContain("instances", text);
        Assert.DoesNotContain("\"help\"", text);
        Assert.DoesNotContain(":  ", text);

        using var doc = JsonDocument.Parse(text);
        var root = doc.RootElement;
        var names = root.EnumerateObject().Select(p => p.Name).ToArray();
        Assert.Equal(["source", "machine", "utc", "allowList", "categoryCount", "categories"], names);
        Assert.Equal(["Paging File"], root.GetProperty("allowList").EnumerateArray().Select(e => e.GetString()).ToArray());
        var cat = root.GetProperty("categories")[0];
        Assert.Equal(["category", "identifier", "type", "counters"], cat.EnumerateObject().Select(p => p.Name).ToArray());
        Assert.Equal("Paging File", cat.GetProperty("category").GetString());
    }

    private static string FindNamed(string name, string mustContain)
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            var hit = dir.GetFiles(name, SearchOption.AllDirectories)
                .FirstOrDefault(f => f.FullName.Contains(mustContain, StringComparison.OrdinalIgnoreCase));
            if (hit is not null)
                return hit.FullName;
            dir = dir.Parent;
        }
        throw new FileNotFoundException(name);
    }
}
