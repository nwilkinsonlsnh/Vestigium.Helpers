using System.Net;
using System.Net.NetworkInformation;
using Vestigium.Helpers.Network;

namespace Vestigium.Helpers.Tests;

public sealed class EgressBindTests
{
    [Fact]
    public void Omitted_index_is_not_rewritten_to_one()
    {
        var echo = new IcmpEchoOptions();
        Assert.Equal(0, echo.InterfaceIndex);
        EgressBind.Validate(0, null);
        Assert.False(EgressBind.IsPinned(0, null));
    }

    [Fact]
    public void Negative_index_is_rejected()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            NetworkHelper.IcmpEcho("127.0.0.1", new IcmpEchoOptions { Count = 1, InterfaceIndex = -1 }));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            NetworkHelper.IcmpTrace("127.0.0.1", new IcmpTraceOptions { InterfaceIndex = -3 }));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            EgressBind.Validate(-1, null));
    }

    [Fact]
    public void Missing_adapter_index_is_rejected()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            EgressBind.Validate(int.MaxValue, null));
    }

    [Fact]
    public void Garbage_source_is_rejected()
    {
        Assert.Throws<ArgumentException>(() =>
            NetworkHelper.IcmpEcho("127.0.0.1", new IcmpEchoOptions { Count = 1, SourceAddress = "not-an-ip" }));
    }

    [Fact]
    public void Source_not_on_host_is_rejected()
    {
        Assert.Throws<ArgumentException>(() =>
            EgressBind.Validate(0, "203.0.113.1"));
    }

    [Fact]
    public void Source_on_wrong_adapter_is_rejected()
    {
        NetworkInterface? first = null;
        UnicastIPAddressInformation? addr = null;
        NetworkInterface? other = null;
        foreach (var nic in NetworkInterface.GetAllNetworkInterfaces())
        {
            var uni = nic.GetIPProperties().UnicastAddresses
                .FirstOrDefault(u => u.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork
                    && !IPAddress.IsLoopback(u.Address));
            if (uni is null)
                continue;
            if (first is null)
            {
                first = nic;
                addr = uni;
                continue;
            }

            other = nic;
            break;
        }

        if (first is null || addr is null || other is null)
            return;

        int otherIndex;
        try
        {
            otherIndex = other.GetIPProperties().GetIPv4Properties().Index;
        }
        catch (NetworkInformationException)
        {
            return;
        }

        if (otherIndex < 1)
            return;

        Assert.Throws<ArgumentException>(() =>
            EgressBind.Validate(otherIndex, addr.Address.ToString()));
    }

    [Fact]
    public void Local_source_on_matching_adapter_is_accepted()
    {
        foreach (var nic in NetworkInterface.GetAllNetworkInterfaces())
        {
            int index;
            try
            {
                index = nic.GetIPProperties().GetIPv4Properties().Index;
            }
            catch (NetworkInformationException)
            {
                continue;
            }

            if (index < 1)
                continue;

            var uni = nic.GetIPProperties().UnicastAddresses
                .FirstOrDefault(u => u.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork);
            if (uni is null)
                continue;

            EgressBind.Validate(index, uni.Address.ToString());
            Assert.True(EgressBind.IsPinned(index, uni.Address.ToString()));
            return;
        }
    }

    [Fact]
    public void Echo_options_default_leave_stack_choice()
    {
        var job = NetworkHelper.IcmpEcho("127.0.0.1", new IcmpEchoOptions { Count = 1, Interval = TimeSpan.Zero });
        Assert.Equal("icmpEcho", job.Kind);
    }
}
