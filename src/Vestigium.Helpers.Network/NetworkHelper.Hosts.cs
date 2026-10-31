using System.Buffers.Binary;
using System.Net;
using System.Net.Sockets;

namespace Vestigium.Helpers.Network;

public static partial class NetworkHelper
{
    public const int HostListCap = 1024;

    public static IReadOnlyList<string> HostsFromPrefix(string cidr)
    {
        var block = DescribePrefix(cidr);
        if (block.Family != AddressFamily.InterNetwork)
            throw new ArgumentException("HostsFromPrefix is IPv4. IPv6 prefixes are not listed.", nameof(cidr));
        if (block.FirstUsable is null || block.LastUsable is null)
            return [];

        var start = ToUint(IPAddress.Parse(block.FirstUsable));
        var end = ToUint(IPAddress.Parse(block.LastUsable));
        var count = (long)end - start + 1;
        if (count > HostListCap)
            throw new ArgumentOutOfRangeException(nameof(cidr), $"Prefix expands to {count} hosts. Cap is {HostListCap}.");

        var hosts = new string[count];
        for (var i = 0; i < count; i++)
            hosts[i] = FromUint(start + (uint)i).ToString();
        return hosts;
    }

    private static uint ToUint(IPAddress address)
        => BinaryPrimitives.ReadUInt32BigEndian(address.GetAddressBytes());

    private static IPAddress FromUint(uint value)
    {
        Span<byte> bytes = stackalloc byte[4];
        BinaryPrimitives.WriteUInt32BigEndian(bytes, value);
        return new IPAddress(bytes);
    }
}
