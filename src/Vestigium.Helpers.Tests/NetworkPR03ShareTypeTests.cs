using Vestigium.Helpers.FileIo;
using Vestigium.Helpers.Network;

namespace Vestigium.Helpers.Tests;

public sealed class NetworkPR03ShareTypeTests : IDisposable
{
    readonly string _root = Path.Combine(Path.GetTempPath(), "pr03-share-" + Guid.NewGuid().ToString("N"));

    public NetworkPR03ShareTypeTests()
    {
        Directory.CreateDirectory(_root);
        NetworkTestHooks.CampaignRoot = _root;
        NetworkTestHooks.ShareRoot = _root;
    }

    public void Dispose()
    {
        NetworkTestHooks.Reset();
        try { Directory.Delete(_root, true); } catch { }
    }

    [Fact]
    public void PR03_002_create_and_open_round_trip()
    {
        var share = Path.Combine(_root, "share");
        Directory.CreateDirectory(share);
        var recipe = Path.Combine(_root, "recipe.json");
        var created = NetworkHelper.CreateShareCampaign(new ShareCampaignOptions
        {
            Target = new FileShareTarget { Directory = share },
            PlannedSize = NetworkHelper.Bandwidth(1, DataUnit.GiB),
            Mode = ShareCampaignMode.Default,
            RecipePath = recipe,
            ResultsPath = Path.Combine(_root, "results.jsonl")
        });

        Assert.StartsWith("share-", created.CampaignId);
        Assert.True(File.Exists(recipe));
        Assert.DoesNotContain("password", File.ReadAllText(recipe), StringComparison.OrdinalIgnoreCase);

        var opened = NetworkHelper.OpenShareCampaign(recipe);
        Assert.Equal(created.CampaignId, opened.CampaignId);
        Assert.Equal(ShareCampaignMode.Default, opened.Options.Mode);
        Assert.Equal(1.0, opened.Options.Efficiency);
    }

    [Fact]
    public void PR03_002_share_path_escape_rejected()
    {
        var outside = Path.Combine(Path.GetTempPath(), "pr03-outside-" + Guid.NewGuid().ToString("N"));
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

    [Fact]
    public void PR03_002_target_has_no_password_field()
    {
        Assert.Null(typeof(FileShareTarget).GetProperty("Password"));
        Assert.Null(typeof(ShareCampaignOptions).GetProperty("Password"));
        Assert.NotNull(typeof(FileShareTarget).GetProperty("Directory"));
    }

    [Fact]
    public void PR03_002_plan_share_probe_accepts_fileio_analysis()
    {
        var source = Path.Combine(_root, "src");
        Directory.CreateDirectory(source);
        File.WriteAllBytes(Path.Combine(source, "a.bin"), new byte[1024]);
        var analysis = FileIoHelper.AnalyzeDirectory(source);
        var plan = NetworkHelper.PlanShareProbe(analysis);
        Assert.Equal(ShareCampaignMode.Advanced, plan.Mode);
        Assert.Equal(1024, plan.PlannedBytes);
        Assert.Empty(plan.Probes);
    }

    [Fact]
    public void PR03_002_run_is_reserved_for_003()
    {
        var share = Path.Combine(_root, "later");
        Directory.CreateDirectory(share);
        var campaign = NetworkHelper.CreateShareCampaign(new ShareCampaignOptions
        {
            Target = new FileShareTarget { Directory = share },
            PlannedSize = NetworkHelper.Bandwidth(64, DataUnit.MiB)
        });
        var ex = Assert.ThrowsAsync<InvalidOperationException>(() => campaign.RunAsync());
        Assert.Contains("PR03.003", ex.Result.Message);
    }
}
