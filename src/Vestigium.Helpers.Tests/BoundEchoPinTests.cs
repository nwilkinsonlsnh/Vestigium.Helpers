using System.Net;
using System.Net.Sockets;
using Vestigium.Helpers.Network;

namespace Vestigium.Helpers.Tests;

public sealed class BoundEchoPinTests
{
    [Fact]
    public void Unpinned_socket_holds()
    {
        using var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        Assert.True(BoundIcmpEcho.PinHolds(socket, 0, null));
    }

    [Fact]
    public void Loopback_source_holds_after_bind()
    {
        using var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        socket.Bind(new IPEndPoint(IPAddress.Loopback, 0));
        Assert.True(BoundIcmpEcho.PinHolds(socket, 0, "127.0.0.1"));
        Assert.False(BoundIcmpEcho.PinHolds(socket, 0, "127.0.0.2"));
    }
}
