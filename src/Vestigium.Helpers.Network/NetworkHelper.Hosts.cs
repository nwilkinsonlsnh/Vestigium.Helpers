using System.Net;
using System.Net.Sockets;
using System.Numerics;

namespace Vestigium.Helpers.Network;

public static partial class NetworkHelper
{
    public const int HostListCap = 1024;

    public static IReadOnlyList<string> HostsFromPrefix(string cidr)
    {
        var block = DescribePrefix(cidr);
        if (block.FirstUsable is null || block.LastUsable is null)
            return [];

        var start = ToInt(IPAddress.Parse(block.FirstUsable));
        var end = ToInt(IPAddress.Parse(block.LastUsable));
        var count = end - start + 1;
        if (count > HostListCap)
            throw new ArgumentOutOfRangeException(nameof(cidr), $"Prefix expands to {count} hosts. Cap is {HostListCap}.");

        var hosts = new string[(int)count];
        for (var i = 0; i < hosts.Length; i++)
            hosts[i] = ToIp(block.Family, start + i).ToString();
        return hosts;
    }

    private static BigInteger ToInt(IPAddress address)
        => new(address.GetAddressBytes(), isUnsigned: true, isBigEndian: true);

    private static IPAddress ToIp(AddressFamily family, BigInteger value)
    {
        var width = family == AddressFamily.InterNetwork ? 4 : 16;
        var bytes = value.ToByteArray(isUnsigned: true, isBigEndian: true);
        if (bytes.Length > width)
            bytes = bytes[^width..];
        else if (bytes.Length < width)
        {
            var padded = new byte[width];
            bytes.CopyTo(padded.AsSpan(width - bytes.Length));
            bytes = padded;
        }

        return new IPAddress(bytes);
    }
}
