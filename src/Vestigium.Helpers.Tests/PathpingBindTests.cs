using Vestigium.Helpers.Network;

namespace Vestigium.Helpers.Tests;

public sealed class PathpingBindTests
{
    [Fact]
    public void Udp_probe_applies_egress_bind()
    {
        var root = Find("IcmpTraceEngine.cs");
        var src = File.ReadAllText(root);
        Assert.Contains("EgressBind.Apply(socket, interfaceIndex, sourceAddress)", src, StringComparison.Ordinal);
        Assert.Contains("int interfaceIndex = 0", src, StringComparison.Ordinal);
    }

    [Fact]
    public void Pathping_sample_passes_bind()
    {
        var root = Find("PathpingEngine.cs");
        var src = File.ReadAllText(root);
        Assert.Contains("UdpProbeAsync(hop.Address, timeoutMs, 64, i, token, options.Family, options.InterfaceIndex, options.SourceAddress)", src, StringComparison.Ordinal);
    }

    [Fact]
    public void Negative_index_rejected_at_pathping_create()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            NetworkHelper.Pathping("127.0.0.1", new PathpingOptions { InterfaceIndex = -1, MaxHops = 1, SamplesPerHop = 1 }));
    }

    private static string Find(string name)
    {
        for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir is not null; dir = dir.Parent)
        {
            var hit = Path.Combine(dir.FullName, "src", "Vestigium.Helpers.Network", name);
            if (File.Exists(hit))
                return hit;
        }

        throw new FileNotFoundException(name);
    }
}
