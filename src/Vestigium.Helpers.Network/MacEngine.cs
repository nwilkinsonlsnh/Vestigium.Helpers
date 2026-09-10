using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Vestigium.Helpers;

namespace Vestigium.Helpers.Network;

internal static class MacEngine
{
    public static MacAddress Parse(string text)
    {
        var raw = HelperGuard.NotBlank(text, nameof(text)).Trim();
        var octets = ReadOctets(raw);
        return FromOctets(octets);
    }

    public static MacAddress FromInteger(ulong value, EuiKind kind)
    {
        var width = kind == EuiKind.Eui48 ? 6 : 8;
        var max = kind == EuiKind.Eui48 ? (1UL << 48) - 1 : ulong.MaxValue;
        if (value > max)
        {
            HelperLog.Reject(HelperLog.AppIds.Network, "Address", nameof(FromInteger), $"value={value} kind={kind}");
            throw new ArgumentOutOfRangeException(nameof(value), "Integer does not fit the EUI width.");
        }

        var octets = new byte[width];
        for (var i = width - 1; i >= 0; i--)
        {
            octets[i] = (byte)(value & 0xFF);
            value >>= 8;
        }

        return FromOctets(octets);
    }

    public static string Format(MacAddress mac, MacFormat format)
        => format switch
        {
            MacFormat.Colon => mac.Colon,
            MacFormat.Hyphen => mac.Hyphen,
            MacFormat.Cisco => mac.Cisco,
            MacFormat.Bare => mac.Bare,
            MacFormat.Integer => mac.Integer.ToString(CultureInfo.InvariantCulture),
            _ => mac.Colon
        };

    public static MacAddress ToModifiedEui64(MacAddress mac)
    {
        if (mac.Kind == EuiKind.Eui64)
            return mac;
        return FromOctets(Expand48(mac.Octets));
    }

    public static MacAddress ToEui48(MacAddress mac)
    {
        if (mac.Kind == EuiKind.Eui48)
            return mac;
        if (mac.Octets.Count != 8 || mac.Octets[3] != 0xFF || mac.Octets[4] != 0xFE)
        {
            HelperLog.Reject(HelperLog.AppIds.Network, "Address", nameof(ToEui48), mac.Colon);
            throw new InvalidOperationException("EUI-64 has no FF:FE in the middle; cannot collapse to EUI-48.");
        }

        var compact = new byte[]
        {
            (byte)(mac.Octets[0] ^ 0x02),
            mac.Octets[1],
            mac.Octets[2],
            mac.Octets[5],
            mac.Octets[6],
            mac.Octets[7]
        };
        return FromOctets(compact);
    }

    public static async Task<OuiLookupResult> LookupOuiAsync(string macOrOui, OuiLookupOptions? options, CancellationToken cancel)
    {
        var parsed = Parse(macOrOui);
        var o = options ?? new OuiLookupOptions();
        var timeout = o.Timeout <= TimeSpan.Zero ? TimeSpan.FromSeconds(3) : o.Timeout;
        var oui = parsed.Oui24;
        var url = (string.IsNullOrWhiteSpace(o.RegistryUrl) ? OuiLookupOptions.DefaultRegistryUrl : o.RegistryUrl)
            .Replace("{oui}", oui, StringComparison.OrdinalIgnoreCase)
            .Replace("{mac}", parsed.Colon, StringComparison.OrdinalIgnoreCase);

        try
        {
            using var client = o.Handler is null ? new HttpClient() : new HttpClient(o.Handler, disposeHandler: false);
            client.Timeout = timeout;
            using var response = await client.GetAsync(url, cancel).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                NetworkLog.Warning("Address", $"oui lookup http={(int)response.StatusCode} oui={oui}");
                return new OuiLookupResult(parsed.Colon, null, OuiSource.None, OuiLookupOptions.Disclaimer);
            }

            var body = (await response.Content.ReadAsStringAsync(cancel).ConfigureAwait(false)).Trim();
            if (body.Length == 0 || body.Contains("<", StringComparison.Ordinal))
                return new OuiLookupResult(parsed.Colon, null, OuiSource.None, OuiLookupOptions.Disclaimer);

            var vendor = body.Length > 200 ? body[..200].Trim() : body;
            NetworkLog.Success("Address", $"oui={oui} vendor-len={vendor.Length}");
            return new OuiLookupResult(parsed.Colon, vendor, OuiSource.Live, OuiLookupOptions.Disclaimer);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or TimeoutException or UriFormatException)
        {
            NetworkLog.Warning("Address", $"oui lookup failed oui={oui} {ex.GetType().Name}");
            return new OuiLookupResult(parsed.Colon, null, OuiSource.None, OuiLookupOptions.Disclaimer);
        }
    }

    static MacAddress FromOctets(IReadOnlyList<byte> octets)
    {
        if (octets.Count is not (6 or 8))
        {
            HelperLog.Reject(HelperLog.AppIds.Network, "Address", nameof(FromOctets), $"len={octets.Count}");
            throw new ArgumentException("MAC must be EUI-48 (6 octets) or EUI-64 (8 octets).");
        }

        var copy = octets.ToArray();
        var kind = copy.Length == 6 ? EuiKind.Eui48 : EuiKind.Eui64;
        var colon = Join(copy, ':');
        var hyphen = Join(copy, '-');
        var bare = Convert.ToHexString(copy);
        var cisco = Cisco(copy);
        ulong integer = 0;
        foreach (var b in copy)
            integer = (integer << 8) | b;

        var first = copy[0];
        var multicast = (first & 0x01) != 0;
        var local = (first & 0x02) != 0;
        var broadcast = copy.All(b => b == 0xFF);
        var unspecified = copy.All(b => b == 0);
        var oui = $"{copy[0]:X2}:{copy[1]:X2}:{copy[2]:X2}";
        string? eui64 = null;
        string? link = null;
        if (kind == EuiKind.Eui48)
        {
            var expanded = Expand48(copy);
            eui64 = Join(expanded, ':');
            link = LinkLocal(expanded);
        }
        else
        {
            eui64 = colon;
            link = LinkLocal(copy);
        }

        return new MacAddress(kind, copy, colon, hyphen, cisco, bare, integer, multicast, local, broadcast, unspecified, oui, eui64, link);
    }

    static byte[] Expand48(IReadOnlyList<byte> mac48)
        =>
        [
            (byte)(mac48[0] ^ 0x02),
            mac48[1],
            mac48[2],
            0xFF,
            0xFE,
            mac48[3],
            mac48[4],
            mac48[5]
        ];

    static string LinkLocal(IReadOnlyList<byte> eui64)
    {
        var bytes = new byte[16];
        bytes[0] = 0xFE;
        bytes[1] = 0x80;
        for (var i = 0; i < 8; i++)
            bytes[8 + i] = eui64[i];
        return new IPAddress(bytes).ToString();
    }

    static string Join(IReadOnlyList<byte> octets, char sep)
    {
        var sb = new StringBuilder(octets.Count * 3);
        for (var i = 0; i < octets.Count; i++)
        {
            if (i > 0) sb.Append(sep);
            sb.Append(octets[i].ToString("X2", CultureInfo.InvariantCulture));
        }
        return sb.ToString();
    }

    static string Cisco(IReadOnlyList<byte> octets)
    {
        var bare = Convert.ToHexString(octets.ToArray());
        var parts = new List<string>();
        for (var i = 0; i < bare.Length; i += 4)
            parts.Add(bare.Substring(i, Math.Min(4, bare.Length - i)));
        return string.Join('.', parts);
    }

    static byte[] ReadOctets(string raw)
    {
        if (IPAddress.TryParse(raw, out var ip) && ip.AddressFamily == AddressFamily.InterNetworkV6)
        {
            var v6 = ip.GetAddressBytes();
            var iid = new byte[8];
            Buffer.BlockCopy(v6, 8, iid, 0, 8);
            return iid;
        }

        var trimmed = raw.Trim();
        if (trimmed.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
        {
            if (!ulong.TryParse(trimmed[2..], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var hexInt))
                Bad(raw);
            return FromIntGuess(hexInt, trimmed[2..].Length);
        }

        if (trimmed.All(char.IsDigit) && ulong.TryParse(trimmed, NumberStyles.Integer, CultureInfo.InvariantCulture, out var dec))
            return FromIntGuess(dec, 0);

        var hex = new StringBuilder();
        foreach (var ch in trimmed)
        {
            if (ch is ':' or '-' or '.' or ' ')
                continue;
            hex.Append(ch);
        }

        var digits = hex.ToString();
        if (digits.Length is not (12 or 16) || !digits.All(Uri.IsHexDigit))
            Bad(raw);

        return Convert.FromHexString(digits);
    }

    static byte[] FromIntGuess(ulong value, int hexDigits)
    {
        var kind = hexDigits > 12 || value > (1UL << 48) - 1 ? EuiKind.Eui64 : EuiKind.Eui48;
        return FromInteger(value, kind).Octets.ToArray();
    }

    static void Bad(string raw)
    {
        HelperLog.Reject(HelperLog.AppIds.Network, "Address", nameof(Parse), "unparsable");
        throw new ArgumentException($"'{raw}' is not an EUI-48, EUI-64, integer, or IPv6 interface id.");
    }
}
