using Vestigium.Helpers.Json;
using Vestigium.Helpers.Network;

namespace Vestigium.Helpers.Tests;

public sealed class NetworkPortableTests : IDisposable
{
    readonly string _root = Path.Combine(Path.GetTempPath(), "VestigiumNetworkHarden", Guid.NewGuid().ToString("N"));

    public NetworkPortableTests()
    {
        Directory.CreateDirectory(_root);
        NetworkTestHooks.CampaignRoot = _root;
    }

    public void Dispose() => NetworkTestHooks.Reset();

    [Fact]
    public void Probe_returns_identity()
    {
        Assert.Equal("Vestigium.Helpers.Network", NetworkHelper.Probe());
        Assert.Equal(NetworkHelper.Identity, NetworkHelper.Probe());
    }

    [Fact]
    public void Campaign_default_root_is_injected_temp_not_programdata()
    {
        Assert.DoesNotContain("ProgramData", _root, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("/var/lib/vestigium", _root, StringComparison.OrdinalIgnoreCase);
        Assert.StartsWith(Path.GetTempPath().TrimEnd(Path.DirectorySeparatorChar), _root, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task One_shot_echo_stats_path_appends_jsonl()
    {
        var path = Path.Combine(_root, "oneshot.jsonl");
        var job = NetworkHelper.IcmpEcho("127.0.0.1", new IcmpEchoOptions
        {
            Count = 1,
            Timeout = TimeSpan.FromMilliseconds(400),
            Interval = TimeSpan.Zero,
            StatsPath = path
        });
        await job.RunAsync();
        Assert.True(File.Exists(path));
        using var session = JsonHelper.OpenJsonl(path);
        Assert.True(session.RecordCount >= 1);
    }

    [Fact]
    public void DeleteRoute_is_remove_alias()
    {
        if (OperatingSystem.IsLinux())
        {
            Assert.Throws<PlatformNotSupportedException>(() =>
                NetworkHelper.DeleteRoute(new NetworkRouteChange
                {
                    Destination = "192.0.2.1",
                    PrefixLength = 32,
                    Gateway = "127.0.0.1"
                }));
        }
    }
}
