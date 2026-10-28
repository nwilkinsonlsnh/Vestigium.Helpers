using Vestigium.Helpers.Network;

namespace Vestigium.Helpers.Tests;

public sealed class AdapterMtuTests
{
    [Fact]
    public void Local_adapter_already_exposes_mtu()
    {
        var adapter = new NetworkAdapter(
            "id",
            "Ethernet",
            "desc",
            System.Net.NetworkInformation.NetworkInterfaceType.Ethernet,
            System.Net.NetworkInformation.OperationalStatus.Up,
            null,
            1_000_000_000,
            true,
            [],
            [],
            [],
            new DhcpInfo(false, null, null, null),
            NetbiosOverTcp.Unknown,
            1,
            null,
            null,
            null,
            1500,
            null,
            [],
            null,
            true,
            null,
            true,
            true);

        Assert.Equal(1500, adapter.Mtu);
    }

    [Fact]
    public void Remote_frame_size_stays_on_path_mtu()
    {
        var path = new PathMtuResult("pmtu-test", "192.0.2.1", "192.0.2.1", NetworkJobStatus.Success, 1472, true, []);
        Assert.Equal(1472, path.LargestPayload);
    }
}
