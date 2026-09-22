using Vestigium.Helpers;

namespace Vestigium.Helpers.Network;

internal static class DnsWireName
{
    public const int MaxLabelOctets = 63;
    public const int MaxEncodedOctets = 255;

    public static void Guard(string name)
    {
        ArgumentNullException.ThrowIfNull(name);
        var encoded = 0;
        foreach (var label in name.Split('.', StringSplitOptions.RemoveEmptyEntries))
        {
            if (label.Length > MaxLabelOctets)
            {
                HelperLog.Reject(HelperLog.AppIds.Network, HelperLog.Subcategories.Dns, nameof(Guard), $"label={label.Length}");
                throw new ArgumentException("DNS label exceeds 63 octets.", nameof(name));
            }

            if (label.Any(ch => ch == 0 || ch > 127))
            {
                HelperLog.Reject(HelperLog.AppIds.Network, HelperLog.Subcategories.Dns, nameof(Guard), "non-ascii");
                throw new ArgumentException("DNS wire names must be ASCII or already-punycode. IDNA is the host's job.", nameof(name));
            }

            encoded += 1 + label.Length;
            if (encoded + 1 <= MaxEncodedOctets) continue;
            HelperLog.Reject(HelperLog.AppIds.Network, HelperLog.Subcategories.Dns, nameof(Guard), $"encoded={encoded + 1}");
            throw new ArgumentException("DNS name exceeds 255 octets on the wire.", nameof(name));
        }
    }
}
