using System.Net;
using System.Net.Sockets;

namespace Vestigium.Helpers.Network;

internal static class OuiLookupGuard
{
    public const int MaxBodyBytes = 4096;
    public const string DefaultRegistryHost = "api.macvendors.com";

    public static Uri Bind(string expandedUrl, OuiLookupOptions options)
    {
        var raw = HelperGuard.NotBlank(expandedUrl, nameof(options.RegistryUrl));
        if (!Uri.TryCreate(raw, UriKind.Absolute, out var uri))
        {
            HelperLog.Reject(HelperLog.AppIds.Network, HelperLog.Subcategories.Address, nameof(Bind), "registry url is not absolute");
            throw new ArgumentException("OUI registry URL must be an absolute HTTPS URI.", nameof(options.RegistryUrl));
        }

        if (!string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
        {
            HelperLog.Reject(HelperLog.AppIds.Network, HelperLog.Subcategories.Address, nameof(Bind), "scheme=" + uri.Scheme);
            throw new ArgumentException("OUI registry URL must use HTTPS.", nameof(options.RegistryUrl));
        }

        if (!string.IsNullOrEmpty(uri.UserInfo))
        {
            HelperLog.Reject(HelperLog.AppIds.Network, HelperLog.Subcategories.Address, nameof(Bind), "userinfo");
            throw new ArgumentException("OUI registry URL must not contain user information.", nameof(options.RegistryUrl));
        }

        var host = uri.IdnHost;
        if (string.IsNullOrWhiteSpace(host))
        {
            HelperLog.Reject(HelperLog.AppIds.Network, HelperLog.Subcategories.Address, nameof(Bind), "missing host");
            throw new ArgumentException("OUI registry URL must include a host.", nameof(options.RegistryUrl));
        }

        if (IsBlockedHost(host))
        {
            HelperLog.Reject(HelperLog.AppIds.Network, HelperLog.Subcategories.Address, nameof(Bind), "blocked host");
            throw new ArgumentException("OUI registry host is not allowed.", nameof(options.RegistryUrl));
        }

        if (!IsAllowedHost(host, options))
        {
            HelperLog.Reject(HelperLog.AppIds.Network, HelperLog.Subcategories.Address, nameof(Bind), "host not allowlisted");
            throw new ArgumentException("Custom OUI registry host is not allowlisted.", nameof(options.RegistryUrl));
        }

        if (options.Handler is null)
            RejectResolvedPrivate(host);

        return uri;
    }

    public static bool IsAllowedHost(string host, OuiLookupOptions options)
    {
        if (string.Equals(host, DefaultRegistryHost, StringComparison.OrdinalIgnoreCase))
            return true;
        if (!options.AllowCustomRegistry)
            return false;
        return options.AllowedRegistryHosts.Any(allowed =>
            !string.IsNullOrWhiteSpace(allowed)
            && string.Equals(allowed.Trim(), host, StringComparison.OrdinalIgnoreCase));
    }

    public static bool IsBlockedHost(string host)
    {
        if (string.Equals(host, "localhost", StringComparison.OrdinalIgnoreCase)
            || host.EndsWith(".localhost", StringComparison.OrdinalIgnoreCase)
            || string.Equals(host, "metadata.google.internal", StringComparison.OrdinalIgnoreCase))
            return true;

        if (IPAddress.TryParse(host.Trim('[').Trim(']'), out var ip))
            return IsBlockedAddress(ip);

        return false;
    }

    public static bool IsBlockedAddress(IPAddress ip)
    {
        if (IPAddress.IsLoopback(ip))
            return true;
        if (ip.IsIPv4MappedToIPv6)
            return IsBlockedAddress(ip.MapToIPv4());

        if (ip.AddressFamily == AddressFamily.InterNetwork)
        {
            var b = ip.GetAddressBytes();
            if (b[0] == 10)
                return true;
            if (b[0] == 127)
                return true;
            if (b[0] == 0)
                return true;
            if (b[0] == 169 && b[1] == 254)
                return true;
            if (b[0] == 192 && b[1] == 168)
                return true;
            if (b[0] == 172 && b[1] >= 16 && b[1] <= 31)
                return true;
            if (b[0] == 100 && b[1] >= 64 && b[1] <= 127)
                return true;
            return false;
        }

        if (ip.AddressFamily == AddressFamily.InterNetworkV6)
        {
            if (ip.IsIPv6LinkLocal)
                return true;
            var bytes = ip.GetAddressBytes();
            if (bytes[0] == 0xFD && bytes[1] == 0x00 && bytes[2] == 0xEC && bytes[3] == 0x02)
                return true;
            if ((bytes[0] & 0xFE) == 0xFC)
                return true;
            return false;
        }

        return true;
    }

    private static void RejectResolvedPrivate(string host)
    {
        if (IPAddress.TryParse(host.Trim('[').Trim(']'), out _))
            return;

        IPAddress[] addresses;
        try
        {
            addresses = Dns.GetHostAddresses(host);
        }
        catch (SocketException)
        {
            return;
        }

        if (addresses.Any(IsBlockedAddress))
        {
            HelperLog.Reject(HelperLog.AppIds.Network, HelperLog.Subcategories.Address, nameof(Bind), "resolved private");
            throw new ArgumentException("OUI registry host resolves to a blocked address.", nameof(OuiLookupOptions.RegistryUrl));
        }
    }
}
