using System.Net;
using System.Net.Sockets;

namespace Vestigium.Helpers.Watch.Dns;

public static class OutboundFrame
{
    public static bool TryRead(byte[] frame, out byte[] question)
    {
        question = [];
        if (frame.Length < 20 || (frame[0] >> 4) != 4)
            return false;

        var header = (frame[0] & 0x0F) * 4;
        if (header < 20 || frame.Length < header + 8)
            return false;
        if (frame[9] != 17)
            return false;

        var source = (frame[header] << 8) | frame[header + 1];
        var destination = (frame[header + 2] << 8) | frame[header + 3];
        if (source != PacketWatch.DnsPort && destination != PacketWatch.DnsPort)
            return false;

        var length = (frame[header + 4] << 8) | frame[header + 5];
        if (length < 8)
            return false;
        var payload = Math.Min(length - 8, frame.Length - (header + 8));
        if (payload < 12)
            return false;

        question = frame[(header + 8)..(header + 8 + payload)];
        return true;
    }

    public static IReadOnlyList<Socket> Open()
    {
        var sockets = new List<Socket>();
        foreach (var address in System.Net.Dns.GetHostAddresses(System.Net.Dns.GetHostName()))
        {
            if (address.AddressFamily != AddressFamily.InterNetwork || IPAddress.IsLoopback(address))
                continue;
            try
            {
                var socket = new Socket(AddressFamily.InterNetwork, SocketType.Raw, ProtocolType.IP);
                socket.Bind(new IPEndPoint(address, 0));
                socket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.HeaderIncluded, true);
                socket.IOControl(IOControlCode.ReceiveAll, [1, 0, 0, 0], null);
                sockets.Add(socket);
            }
            catch (Exception)
            {
                continue;
            }
        }

        return sockets;
    }
}
