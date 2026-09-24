using Vestigium.Helpers.Network;

namespace Vestigium.Helpers.Tests;

public sealed class CampaignBindRecipeTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "VestigiumNetworkCampaigns", Guid.NewGuid().ToString("N"));

    public CampaignBindRecipeTests()
    {
        Directory.CreateDirectory(_root);
        NetworkTestHooks.CampaignRoot = _root;
    }

    public void Dispose()
    {
        NetworkTestHooks.Reset();
        try { Directory.Delete(_root, true); } catch { }
    }

    [Fact]
    public void Recipe_writes_source_and_opens_it()
    {
        var recipe = Path.Combine(_root, "bind-recipe.json");
        NetworkHelper.CreateEchoCampaign(new IcmpEchoCampaignOptions
        {
            Target = "127.0.0.1",
            RangeStartDate = new DateOnly(2026, 9, 24),
            RangeEndDate = new DateOnly(2026, 9, 24),
            RecipePath = recipe,
            Windows = [new EchoWindow(new TimeOnly(12, 0), 1)],
            Echo = new IcmpEchoOptions { SourceAddress = "127.0.0.1" }
        });

        var text = File.ReadAllText(recipe);
        Assert.Contains("127.0.0.1", text, StringComparison.Ordinal);

        var opened = NetworkHelper.OpenEchoCampaign(recipe);
        Assert.Equal("127.0.0.1", opened.Options.Echo.SourceAddress);
        Assert.Equal(0, opened.Options.Echo.InterfaceIndex);
    }

    [Fact]
    public void Old_recipe_without_bind_opens_unset()
    {
        var recipe = Path.Combine(_root, "old-bind.json");
        File.WriteAllText(recipe, """
            {
              "campaignId": "camp-old",
              "target": "127.0.0.1",
              "rangeStartDate": "2026-09-24",
              "rangeEndDate": "2026-09-24",
              "graceMinutes": 15,
              "windows": [ { "localTime": "12:00", "count": 1 } ]
            }
            """);

        var opened = NetworkHelper.OpenEchoCampaign(recipe);
        Assert.Equal(0, opened.Options.Echo.InterfaceIndex);
        Assert.True(string.IsNullOrWhiteSpace(opened.Options.Echo.SourceAddress));
    }
}
