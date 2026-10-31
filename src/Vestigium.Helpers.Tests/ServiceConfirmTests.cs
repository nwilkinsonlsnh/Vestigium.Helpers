using Vestigium.Helpers.Network;

namespace Vestigium.Helpers.Tests;

public sealed class ServiceConfirmTests
{
    [Fact]
    public void Confirm_is_off_by_default()
    {
        Assert.False(new PortScanOptions().Confirm);
        Assert.False(new TcpConnectOptions().ConfirmService);
    }

    [Fact]
    public void Http_ports_have_a_short_probe_and_tls_does_not()
    {
        Assert.NotNull(ServiceConfirm.ProbeFor(80));
        Assert.NotNull(ServiceConfirm.ProbeFor(8080));
        Assert.Null(ServiceConfirm.ProbeFor(443));
        Assert.Null(ServiceConfirm.ProbeFor(22));
    }
}
