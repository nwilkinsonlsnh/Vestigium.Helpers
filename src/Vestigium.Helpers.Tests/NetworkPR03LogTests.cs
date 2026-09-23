using Vestigium.Helpers.Network;

namespace Vestigium.Helpers.Tests;

public sealed class NetworkPr03LogTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "pr03-log-" + Guid.NewGuid().ToString("N"));

    private static readonly string[] Forbidden =
    [
        "password", "passwd", "credential", "secret", "connectionstring", "pwd="
    ];

    public NetworkPr03LogTests()
    {
        Directory.CreateDirectory(_root);
        NetworkTestHooks.CampaignRoot = _root;
        NetworkTestHooks.ShareRoot = _root;
        NetworkTestHooks.ProbeBytesPerSecond = [100_000_000d, 100_000_000d, 100_000_000d, 100_000_000d];
    }

    public void Dispose()
    {
        NetworkTestHooks.Reset();
        try { Directory.Delete(_root, true); } catch { }
    }

    [Fact]
    public void PR03_006_jsonl_has_no_password_keys()
    {
        var share = Path.Combine(_root, "dept-share");
        Directory.CreateDirectory(share);
        var recipe = Path.Combine(_root, "share-recipe.json");
        var results = Path.Combine(_root, "share-results.jsonl");
        var campaign = NetworkHelper.CreateShareCampaign(new ShareCampaignOptions
        {
            Target = new FileShareTarget { Directory = share },
            PlannedSize = NetworkHelper.Bandwidth(1, DataUnit.GiB),
            RecipePath = recipe,
            ResultsPath = results
        });
        campaign.RunAsync().GetAwaiter().GetResult();

        var recipeText = File.ReadAllText(recipe);
        var jsonl = File.ReadAllText(results);
        Assert.All(Forbidden, key =>
        {
            Assert.DoesNotContain(key, recipeText, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain(key, jsonl, StringComparison.OrdinalIgnoreCase);
        });

        Assert.Contains("campaignStart", jsonl, StringComparison.Ordinal);
        Assert.Contains("\"kind\":\"probe\"", jsonl.Replace(" ", ""), StringComparison.OrdinalIgnoreCase);
        Assert.Contains("windowSummary", jsonl, StringComparison.Ordinal);
        Assert.Contains("campaignEnd", jsonl, StringComparison.Ordinal);
        Assert.Contains("dept-share", jsonl, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(_root.Replace("\\", @"\\"), jsonl, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void PR03_006_share_path_escape_rejected()
    {
        var outside = Path.Combine(Path.GetTempPath(), "pr03-log-out-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(outside);
        try
        {
            var ex = Assert.Throws<ArgumentException>(() =>
                NetworkHelper.CreateShareCampaign(new ShareCampaignOptions
                {
                    Target = new FileShareTarget { Directory = outside },
                    PlannedSize = NetworkHelper.Bandwidth(1, DataUnit.GiB)
                }));
            Assert.Contains("share root", ex.Message, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            try { Directory.Delete(outside, true); } catch { }
        }
    }
}
