using Vestigium.Helpers;
using Vestigium.Helpers.Processes;

namespace Vestigium.Helpers.Tests;

[Collection("Logger")]
public sealed class ProcessTaxonomyTests
{
    [Fact]
    public void Processes_subcategories_are_registered()
    {
        string[] required =
        [
            HelperLog.Subcategories.Probe,
            HelperLog.Subcategories.Identity,
            HelperLog.Subcategories.Inventory,
            HelperLog.Subcategories.Process,
            HelperLog.Subcategories.Thread,
            HelperLog.Subcategories.Watch,
            HelperLog.Subcategories.Start,
            HelperLog.Subcategories.Kill,
            HelperLog.Subcategories.System,
            HelperLog.Subcategories.Campaign,
            HelperLog.Subcategories.Jsonl
        ];
        foreach (var sub in required)
            Assert.True(HelperLog.Taxonomy.IsSubcategoryRegistered(HelperLog.Category, sub), sub);
    }

    [Fact]
    public void Identity_is_processes()
        => Assert.Equal("Vestigium.Helpers.Processes", ProcessHelper.Identity);
}
