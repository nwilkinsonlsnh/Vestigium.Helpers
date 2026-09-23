using Vestigium.Helpers.Network;

namespace Vestigium.Helpers.Tests;

public sealed class NetworkPR01CampaignPathTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "vest-pr01-camp-" + Guid.NewGuid().ToString("N"));

    public NetworkPR01CampaignPathTests()
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
    public void PR01_006_results_path_escape_rejected()
    {
        var escape = Path.Combine(_root, "..", "..", "Windows", "evil.jsonl");
        var ex = Assert.Throws<ArgumentException>(() =>
            NetworkHelper.CreateEchoCampaign(new IcmpEchoCampaignOptions
            {
                Target = "127.0.0.1",
                ResultsPath = escape,
                Windows = [new EchoWindow(new TimeOnly(12, 0), 1)]
            }));
        Assert.Contains("campaign root", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void PR01_006_recipe_path_escape_rejected()
    {
        var escape = Path.GetFullPath(Path.Combine(_root, "..", "outside-recipe.json"));
        var ex = Assert.Throws<ArgumentException>(() =>
            NetworkHelper.CreateEchoCampaign(new IcmpEchoCampaignOptions
            {
                Target = "127.0.0.1",
                RecipePath = escape,
                Windows = [new EchoWindow(new TimeOnly(12, 0), 1)]
            }));
        Assert.Contains("campaign root", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void PR01_006_path_under_root_is_allowed()
    {
        var results = Path.Combine(_root, "nested", "run.jsonl");
        var campaign = NetworkHelper.CreateEchoCampaign(new IcmpEchoCampaignOptions
        {
            Target = "127.0.0.1",
            ResultsPath = results,
            Windows = [new EchoWindow(new TimeOnly(12, 0), 1)]
        });
        Assert.StartsWith(Path.GetFullPath(_root), campaign.Options.ResultsPath, StringComparison.OrdinalIgnoreCase);
    }
}
