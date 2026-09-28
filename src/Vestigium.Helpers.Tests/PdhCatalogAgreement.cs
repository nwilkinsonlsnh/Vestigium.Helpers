using System.Reflection;
using System.Text.Json;

namespace Vestigium.Helpers.Tests;

internal static class PdhCatalogAgreement
{
    public static void AssertMatches(
        Assembly probeAssembly,
        string category,
        IReadOnlyList<string> typedCounters,
        IReadOnlyList<string> counterSetKnown)
    {
        var jsonPath = FindShard(probeAssembly);
        Xunit.Assert.True(File.Exists(jsonPath), jsonPath);

        using var doc = JsonDocument.Parse(File.ReadAllText(jsonPath));
        var row = doc.RootElement.GetProperty("categories").EnumerateArray()
            .Single(c => string.Equals(c.GetProperty("category").GetString(), category, StringComparison.OrdinalIgnoreCase));

        var shard = row.GetProperty("counters").EnumerateArray()
            .Select(c => c.GetProperty("name").GetString()!)
            .ToArray();

        Xunit.Assert.Equal(shard, typedCounters);
        Xunit.Assert.Equal(typedCounters, counterSetKnown);
    }

    private static string FindShard(Assembly probeAssembly)
    {
        var probe = probeAssembly.GetName().Name!;
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            var hits = dir.GetFiles("pdh-categories.json", SearchOption.AllDirectories)
                .Where(f => f.FullName.Contains(probe, StringComparison.OrdinalIgnoreCase))
                .ToArray();
            if (hits.Length > 0)
                return hits[0].FullName;
            dir = dir.Parent;
        }

        throw new FileNotFoundException($"pdh-categories.json for {probe}");
    }
}
