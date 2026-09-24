using System.Buffers.Binary;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using Vestigium.Helpers;

namespace Vestigium.Helpers.Network;

internal static class NeighborProbeEngine
{
    public static NeighborProbeResult Probe(string address)
    {
        var text = HelperGuard.NotBlank(address, nameof(address)).Trim();
        if (!IPAddress.TryParse(text, out var ip))
        {
            HelperLog.Reject(HelperLog.AppIds.Network, HelperLog.Subcategories.Neighbor, nameof(Probe), "not an IP");
            throw new ArgumentException("Address must be a single IPv4 or IPv6 address.", nameof(address));
        }

        NetworkLog.Pending(HelperLog.Subcategories.Neighbor, $"probeNeighbor address={ip}");
        var mac = NeighborResolve.Resolve(ip);
        if (string.IsNullOrWhiteSpace(mac) && OperatingSystem.IsWindows() && ip.AddressFamily == AddressFamily.InterNetwork)
            mac = SendArp(ip);
        if (string.IsNullOrWhiteSpace(mac))
        {
            Nudge(ip);
            mac = NeighborResolve.Resolve(ip) ?? SendArpFallback(ip);
        }

        var found = !string.IsNullOrWhiteSpace(mac);
        NetworkLog.Success(HelperLog.Subcategories.Neighbor, $"probeNeighbor address={ip} found={found}");
        return new NeighborProbeResult(ip.ToString(), found ? mac : null, found);
    }

    private static string? SendArpFallback(IPAddress ip)
        => OperatingSystem.IsWindows() && ip.AddressFamily == AddressFamily.InterNetwork ? SendArp(ip) : null;

    private static void Nudge(IPAddress ip)
    {
        try
        {
            using var socket = new Socket(ip.AddressFamily, SocketType.Dgram, ProtocolType.Udp);
            socket.SendTimeout = 50;
            socket.ReceiveTimeout = 50;
            socket.SendTo(new byte[1], new IPEndPoint(ip, 9));
        }
        catch (SocketException)
        {
        }
    }

    private static string? SendArp(IPAddress dest)
    {
        var destBytes = dest.GetAddressBytes();
        if (destBytes.Length != 4)
            return null;
        var destIp = BinaryPrimitives.ReadUInt32LittleEndian(destBytes);
        var mac = new byte[6];
        var length = mac.Length;
        var status = SendARP(destIp, 0, mac, ref length);
        if (status != 0 || length < 6)
            return null;
        if (mac.All(b => b == 0))
            return null;
        return string.Join(':', mac.Take(6).Select(b => b.ToString("X2")));
    }

    [DllImport("iphlpapi.dll", ExactSpelling = true)]
    private static extern int SendARP(uint destIp, uint srcIp, byte[] macAddr, ref int macAddrLen);
}
