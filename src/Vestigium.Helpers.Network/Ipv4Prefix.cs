using System.Net;
using System.Net.Sockets;

namespace Vestigium.Helpers.Network;

/// <summary>
/// Derive IPv4 prefix length and dotted mask so they always agree.
/// Never invent a /24 when the OS did not say so.
/// </summary>
internal static class Ipv4Prefix
{
    public static int PrefixFromMask(IPAddress mask)
    {
        ArgumentNullException.ThrowIfNull(mask);
        var bytes = mask.GetAddressBytes();
        var bits = 0;
        var zero = false;
        foreach (var b in bytes)
        {
            for (var i = 7; i >= 0; i--)
            {
                var on = (b & (1 << i)) != 0;
                if (on)
                {
                    if (zero)
                        return bits;
                    bits++;
                }
                else
                {
                    zero = true;
                }
            }
        }

        return bits;
    }

    public static string MaskFromPrefix(int prefixLength)
    {
        if (prefixLength < 0)
            prefixLength = 0;
        if (prefixLength > 32)
            prefixLength = 32;
        var mask = prefixLength == 0 ? 0u : uint.MaxValue << (32 - prefixLength);
        return $"{(mask >> 24) & 0xFF}.{(mask >> 16) & 0xFF}.{(mask >> 8) & 0xFF}.{mask & 0xFF}";
    }

    public static void Agree(int? prefixLength, IPAddress? mask, out int prefix, out string dottedMask)
    {
        if (prefixLength is >= 0 and <= 32)
        {
            prefix = prefixLength.Value;
            dottedMask = MaskFromPrefix(prefix);
            return;
        }

        if (mask is not null && mask.AddressFamily == AddressFamily.InterNetwork)
        {
            prefix = PrefixFromMask(mask);
            dottedMask = MaskFromPrefix(prefix);
            return;
        }

        prefix = 0;
        dottedMask = MaskFromPrefix(0);
    }
}
