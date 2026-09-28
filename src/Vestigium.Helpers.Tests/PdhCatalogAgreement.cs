using System.Text.Json;

namespace Vestigium.Helpers.Tests;

internal static class PdhCatalogAgreement
{
    public static void AssertMatches(
        System.Reflection.Assembly probeAssembly,
        string category,
        IReadOnlyList<string> typedCounters,
        IReadOnlyList<string> counterSetKnown)
    {
        var jsonPath = Path.Combine(
            Path.GetDirectoryName(probeAssembly.Location)!,
            "EventCatalog",
            "pdh-categories.json");
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
}
