using Vestigium.Helpers.Network;

namespace Vestigium.Helpers.Tests;

public sealed class NetworkPR04OuiTests
{
    [Fact]
    public void PR04_002_packed_oui_resolves_cisco()
    {
        var hit = NetworkHelper.LookupOuiPacked("00:00:0C:11:22:33");
        Assert.Equal(OuiSource.File, hit.Source);
        Assert.Contains("Cisco", hit.Vendor, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("not a live IEEE", hit.Disclaimer, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void PR04_002_packed_oui_unknown_is_none()
    {
        var miss = NetworkHelper.LookupOuiPacked("FF:FF:FF:00:00:01");
        Assert.Equal(OuiSource.None, miss.Source);
        Assert.Null(miss.Vendor);
    }

    [Fact]
    public void PR04_002_packed_registry_is_offline()
    {
        var map = NetworkHelper.LoadPackedOuiRegistry();
        Assert.True(map.Count >= 10);
        Assert.Contains(map.Keys, k => k.Equals("00:00:0C", StringComparison.OrdinalIgnoreCase));
    }
}
