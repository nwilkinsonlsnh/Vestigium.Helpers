using System.Text.Json;
using Vestigium.Helpers.PerfMon.PageFile;

namespace Vestigium.Helpers.Tests;

public sealed class PerfMonPr02aTests
{
    [Fact]
    public void PR02a_001_pagefile_shard_is_allow_listed()
    {
        var jsonPath = Path.Combine(
            Path.GetDirectoryName(typeof(PageFilePerfCatalog).Assembly.Location)!,
            "EventCatalog",
            "pdh-categories.json");
        Assert.True(File.Exists(jsonPath), jsonPath);

        using var doc = JsonDocument.Parse(File.ReadAllText(jsonPath));
        var root = doc.RootElement;
        Assert.Equal(1, root.GetProperty("categoryCount").GetInt32());
        var allow = root.GetProperty("allowList").EnumerateArray().Select(e => e.GetString()).ToArray();
        Assert.Equal(["Paging File"], allow);

        var cats = root.GetProperty("categories").EnumerateArray().ToArray();
        Assert.Single(cats);
        var row = cats[0];
        Assert.Equal("Paging File", row.GetProperty("category").GetString());
        Assert.Equal("PagingFile", row.GetProperty("identifier").GetString());
        Assert.False(row.TryGetProperty("instances", out _));
        Assert.False(row.TryGetProperty("help", out _));

        var counters = row.GetProperty("counters").EnumerateArray().Select(c => (
            c.GetProperty("name").GetString(),
            c.GetProperty("identifier").GetString())).ToArray();
        Assert.Equal(("% Usage", "PercentUsage"), counters[0]);
        Assert.Equal(("% Usage Peak", "PercentUsagePeak"), counters[1]);
    }

    [Fact]
    public void PR02a_001_mini_dump_filter_drops_clr_and_instances()
    {
        var mini = FindFixture("pdh-mini.json");
        using var doc = JsonDocument.Parse(File.ReadAllText(mini));
        var wanted = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "Paging File" };
        var hits = doc.RootElement.GetProperty("categories").EnumerateArray()
            .Where(c => wanted.Contains(c.GetProperty("category").GetString()!))
            .ToArray();
        Assert.Single(hits);
        Assert.Equal("Paging File", hits[0].GetProperty("category").GetString());
        Assert.DoesNotContain(
            doc.RootElement.GetProperty("categories").EnumerateArray().Select(c => c.GetProperty("category").GetString()),
            name => name == "Paging File" ? false : wanted.Contains(name!));
        Assert.Contains(
            doc.RootElement.GetProperty("categories").EnumerateArray().Select(c => c.GetProperty("category").GetString()),
            name => name == ".NET CLR Memory");
    }

    [Fact]
    public void PR02a_002_pagingfile_matches_shard()
    {
        Assert.Equal("Paging File", PagingFile.Category);
        Assert.Equal("% Usage", PagingFile.PercentUsage);
        Assert.Equal("% Usage Peak", PagingFile.PercentUsagePeak);
        Assert.Equal(["% Usage", "% Usage Peak"], PagingFile.Counters);

        var jsonPath = Path.Combine(
            Path.GetDirectoryName(typeof(PageFilePerfCatalog).Assembly.Location)!,
            "EventCatalog",
            "pdh-categories.json");
        using var doc = JsonDocument.Parse(File.ReadAllText(jsonPath));
        var row = doc.RootElement.GetProperty("categories")[0];
        Assert.Equal(PagingFile.Category, row.GetProperty("category").GetString());
        var names = row.GetProperty("counters").EnumerateArray()
            .Select(c => c.GetProperty("name").GetString())
            .ToArray();
        Assert.Equal(PagingFile.Counters, names);
    }

    private static string FindFixture(string name)
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            var hit = dir.GetFiles(name, SearchOption.AllDirectories).FirstOrDefault();
            if (hit is not null)
                return hit.FullName;
            dir = dir.Parent;
        }
        throw new FileNotFoundException(name);
    }
}
