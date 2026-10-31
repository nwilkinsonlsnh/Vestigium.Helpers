using System.Net;
using System.Net.Sockets;

namespace Vestigium.Helpers.Network.RawSocket;

internal static class SynPacket
{
    public static byte[] Build(IPAddress source, IPAddress destination, int sourcePort, int destinationPort, uint seq, byte flags)
    {
        var packet = new byte[20];
        packet[0] = (byte)(sourcePort >> 8);
        packet[1] = (byte)sourcePort;
        packet[2] = (byte)(destinationPort >> 8);
        packet[3] = (byte)destinationPort;
        packet[4] = (byte)(seq >> 24);
        packet[5] = (byte)(seq >> 16);
        packet[6] = (byte)(seq >> 8);
        packet[7] = (byte)seq;
        packet[12] = 5 << 4;
        packet[13] = flags;
        packet[14] = 0xFF;
        packet[15] = 0xFF;
        var sum = Checksum(source, destination, packet);
        packet[16] = (byte)(sum >> 8);
        packet[17] = (byte)sum;
        return packet;
    }

    public static bool TryClassify(byte[] buffer, int length, int localPort, int remotePort, out SynPortStatus status)
    {
        status = SynPortStatus.Filtered;
        if (length < 40)
            return false;
        var ipHeader = (buffer[0] & 0x0F) * 4;
        if (ipHeader < 20 || length < ipHeader + 20)
            return false;
        var tcp = ipHeader;
        var src = (buffer[tcp] << 8) | buffer[tcp + 1];
        var dst = (buffer[tcp + 2] << 8) | buffer[tcp + 3];
        if (src != remotePort || dst != localPort)
            return false;
        var flags = buffer[tcp + 13];
        if ((flags & 0x12) == 0x12)
        {
            status = SynPortStatus.Open;
            return true;
        }

        if ((flags & 0x04) == 0x04)
        {
            status = SynPortStatus.Closed;
            return true;
        }

        return false;
    }

    private static ushort Checksum(IPAddress source, IPAddress destination, byte[] tcp)
    {
        uint sum = 0;
        var src = source.GetAddressBytes();
        var dst = destination.GetAddressBytes();
        sum += (uint)((src[0] << 8) | src[1]);
        sum += (uint)((src[2] << 8) | src[3]);
        sum += (uint)((dst[0] << 8) | dst[1]);
        sum += (uint)((dst[2] << 8) | dst[3]);
        sum += (uint)ProtocolType.Tcp;
        sum += (uint)tcp.Length;
        for (var i = 0; i < tcp.Length; i += 2)
        {
            var word = (uint)(tcp[i] << 8);
            if (i + 1 < tcp.Length)
                word |= tcp[i + 1];
            sum += word;
        }

        while ((sum >> 16) != 0)
            sum = (sum & 0xFFFF) + (sum >> 16);
        return (ushort)~sum;
    }
}
