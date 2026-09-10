using System.Net;

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
        if (prefixLength is < 0 or > 32)
            prefixLength = Math.Clamp(prefixLength, 0, 32);
        var mask = prefixLength == 0 ? 0u : uint.MaxValue << (32 - prefixLength);
        return string.Create(15, mask, static (span, value) =>
        {
            var written = 0;
            WriteOctet(span, ref written, (value >> 24) & 0xFF);
            span[written++] = '.';
            WriteOctet(span, ref written, (value >> 16) & 0xFF);
            span[written++] = '.';
            WriteOctet(span, ref written, (value >> 8) & 0xFF);
            span[written++] = '.';
            WriteOctet(span, ref written, value & 0xFF);
        })[..MaskLength(prefixLength)];
    }

    public static void Agree(int? prefixLength, IPAddress? mask, out int prefix, out string dottedMask)
    {
        if (prefixLength is >= 0 and <= 32)
        {
            prefix = prefixLength.Value;
            dottedMask = MaskFromPrefix(prefix);
            return;
        }

        if (mask is not null && mask.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
        {
            prefix = PrefixFromMask(mask);
            dottedMask = MaskFromPrefix(prefix);
            return;
        }

        prefix = 0;
        dottedMask = MaskFromPrefix(0);
    }

    private static int MaskLength(int prefixLength)
    {
        // "0.0.0.0" = 7, "255.255.255.255" = 15. Cheap upper bound trim via known strings.
        return MaskFromPrefixUntrimmed(prefixLength).Length;
    }

    private static string MaskFromPrefixUntrimmed(int prefixLength)
    {
        var mask = prefixLength == 0 ? 0u : uint.MaxValue << (32 - prefixLength);
        return $"{(mask >> 24) & 0xFF}.{(mask >> 16) & 0xFF}.{(mask >> 8) & 0xFF}.{mask & 0xFF}";
    }

    private static void WriteOctet(Span<char> span, ref int written, uint value)
    {
        var text = value.ToString();
        text.AsSpan().CopyTo(span[written..]);
        written += text.Length;
    }
}
