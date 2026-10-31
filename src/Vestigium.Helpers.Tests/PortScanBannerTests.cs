using Vestigium.Helpers.Network;

namespace Vestigium.Helpers.Tests;

public sealed class PortScanBannerTests
{
    [Fact]
    public void ReadBanner_is_off_by_default()
    {
        Assert.False(new PortScanOptions().ReadBanner);
    }

    [Fact]
    public void Confirm_and_ReadBanner_are_independent()
    {
        var options = new PortScanOptions { Confirm = true };
        Assert.True(options.Confirm);
        Assert.False(options.ReadBanner);
    }
}
