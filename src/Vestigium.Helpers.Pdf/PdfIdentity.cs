using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Security.Principal;

namespace Vestigium.Helpers.Pdf;

/// <summary>Who ran it, where, and when. Capture lists every up adapter. It does not pick a favourite.</summary>
public sealed class PdfIdentity
{
    public string User { get; init; } = "";

    public string? Sid { get; init; }

    public string Machine { get; init; } = "";

    public string? Domain { get; init; }

    public DateTimeOffset Utc { get; init; }

    public string Version { get; init; } = "";

    public IReadOnlyList<PdfAdapter> Adapters { get; init; } = Array.Empty<PdfAdapter>();

    public static PdfIdentity Capture()
    {
        var user = Environment.UserName;
        string? sid = null;
        if (OperatingSystem.IsWindows())
        {
            try
            {
                using var id = WindowsIdentity.GetCurrent();
                if (!string.IsNullOrWhiteSpace(id.Name))
                    user = id.Name;
                sid = id.User?.Value;
            }
            catch (Exception)
            {
                /* name and sid stay at the fallback */
            }
        }

        return new PdfIdentity
        {
            User = user,
            Sid = sid,
            Machine = Environment.MachineName,
            Domain = string.IsNullOrWhiteSpace(Environment.UserDomainName) ? null : Environment.UserDomainName,
            Utc = DateTimeOffset.UtcNow,
            Version = PdfEvidence.Version,
            Adapters = ReadAdapters()
        };
    }

    /// <summary>Fields in seal order, one per line, UTF-8. Adapters follow, one per line.</summary>
    public string Canonical()
    {
        var lines = new List<string>
        {
            "user=" + User,
            "sid=" + (Sid ?? ""),
            "machine=" + Machine,
            "domain=" + (Domain ?? ""),
            "utc=" + Utc.ToString("o"),
            "version=" + Version
        };
        foreach (var adapter in Adapters)
            lines.Add("adapter=" + adapter.Name + "|" + adapter.Address + "|" + adapter.Mac);
        return string.Join('\n', lines);
    }

    private static IReadOnlyList<PdfAdapter> ReadAdapters()
    {
        var found = new List<PdfAdapter>();
        NetworkInterface[] nics;
        try
        {
            nics = NetworkInterface.GetAllNetworkInterfaces();
        }
        catch (Exception)
        {
            return found;
        }

        foreach (var nic in nics)
        {
            if (nic.OperationalStatus != OperationalStatus.Up || nic.NetworkInterfaceType == NetworkInterfaceType.Loopback)
                continue;
            var address = FirstAddress(nic);
            if (address is null)
                continue;
            found.Add(new PdfAdapter
            {
                Name = nic.Name,
                Address = address,
                Mac = Format(nic.GetPhysicalAddress())
            });
        }
        return found;
    }

    private static string? FirstAddress(NetworkInterface nic)
    {
        UnicastIPAddressInformation[] addresses;
        try
        {
            addresses = nic.GetIPProperties().UnicastAddresses.ToArray();
        }
        catch (Exception)
        {
            return null;
        }

        string? v6 = null;
        foreach (var entry in addresses)
        {
            var ip = entry.Address;
            if (IPAddress.IsLoopback(ip) || ip.IsIPv6LinkLocal)
                continue;
            if (ip.AddressFamily == AddressFamily.InterNetwork)
                return ip.ToString();
            v6 ??= ip.AddressFamily == AddressFamily.InterNetworkV6 ? ip.ToString() : null;
        }
        return v6;
    }

    private static string Format(PhysicalAddress address)
    {
        var bytes = address.GetAddressBytes();
        return bytes.Length == 0 ? "" : string.Join("-", bytes.Select(b => b.ToString("X2")));
    }
}
