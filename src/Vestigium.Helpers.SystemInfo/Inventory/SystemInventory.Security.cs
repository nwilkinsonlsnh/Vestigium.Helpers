using System.Management;
using System.Security.Cryptography.X509Certificates;

namespace Vestigium.Helpers.SystemInfo;

public static partial class SystemInventory
{
    private static readonly string[] CertStores = ["My", "Root", "CA", "TrustedPublisher"];
    private static readonly (StoreLocation Location, string Label)[] CertLocations =
    [
        (StoreLocation.LocalMachine, "LM"),
        (StoreLocation.CurrentUser, "CU")
    ];

    /// <summary>
    /// Reads firewall profiles, rules, certificates, and drivers.
    /// Each section is Fact.Unavailable on failure. Partial success is allowed.
    /// No truncation. Host decides the cap.
    /// Numeric CIM values stay numeric. Host maps labels.
    /// </summary>
    public static SecurityInventory ReadSecurity()
    {
        return new SecurityInventory(
            ReadProfiles(),
            ReadRules(),
            ReadCertificates(),
            ReadDrivers());
    }

    private static Fact<IReadOnlyList<FirewallProfile>> ReadProfiles()
    {
        try
        {
            var list = new List<FirewallProfile>();
            foreach (var mo in QueryCim("SELECT Name, Enabled, DefaultInboundAction, DefaultOutboundAction, NotifyOnListen, LogAllowed, LogBlocked FROM MSFT_NetFirewallProfile"))
            {
                using (mo)
                {
                    list.Add(new FirewallProfile(
                        String(mo, "Name") ?? "",
                        UInt(mo, "Enabled"),
                        UInt(mo, "DefaultInboundAction"),
                        UInt(mo, "DefaultOutboundAction"),
                        Bool(mo, "NotifyOnListen"),
                        Bool(mo, "LogAllowed"),
                        Bool(mo, "LogBlocked")));
                }
            }
            return Fact<IReadOnlyList<FirewallProfile>>.Ok(list);
        }
        catch (Exception)
        {
            return Fact<IReadOnlyList<FirewallProfile>>.Unavailable();
        }
    }

    private static Fact<IReadOnlyList<FirewallRule>> ReadRules()
    {
        try
        {
            var list = new List<FirewallRule>();
            foreach (var mo in QueryCim("SELECT DisplayName, Enabled, Direction, Action, Profiles, Protocol FROM MSFT_NetFirewallRule"))
            {
                using (mo)
                {
                    list.Add(new FirewallRule(
                        String(mo, "DisplayName") ?? "",
                        UInt(mo, "Enabled"),
                        UInt(mo, "Direction"),
                        UInt(mo, "Action"),
                        UInt(mo, "Profiles"),
                        String(mo, "Protocol") ?? ""));
                }
            }
            return Fact<IReadOnlyList<FirewallRule>>.Ok(list);
        }
        catch (Exception)
        {
            return Fact<IReadOnlyList<FirewallRule>>.Unavailable();
        }
    }

    private static Fact<IReadOnlyList<CertificateInfo>> ReadCertificates()
    {
        try
        {
            var list = new List<CertificateInfo>();
            var opened = 0;
            foreach (var (location, label) in CertLocations)
            {
                foreach (var name in CertStores)
                {
                    using var store = new X509Store(name, location);
                    try
                    {
                        store.Open(OpenFlags.ReadOnly);
                        opened++;
                    }
                    catch (Exception)
                    {
                        continue;
                    }

                    foreach (var cert in store.Certificates)
                    {
                        list.Add(new CertificateInfo(
                            label,
                            name,
                            cert.Subject,
                            cert.Issuer,
                            cert.Thumbprint,
                            cert.NotAfter.ToUniversalTime(),
                            cert.HasPrivateKey));
                    }
                }
            }
            return opened == 0
                ? Fact<IReadOnlyList<CertificateInfo>>.Unavailable()
                : Fact<IReadOnlyList<CertificateInfo>>.Ok(list);
        }
        catch (Exception)
        {
            return Fact<IReadOnlyList<CertificateInfo>>.Unavailable();
        }
    }

    private static Fact<IReadOnlyList<DriverInfo>> ReadDrivers()
    {
        try
        {
            var list = new List<DriverInfo>();
            foreach (var mo in Query("SELECT DeviceName, DriverVersion, Manufacturer, IsSigned, Signer, InfName FROM Win32_PnPSignedDriver"))
            {
                using (mo)
                {
                    list.Add(new DriverInfo(
                        "PnP",
                        String(mo, "DeviceName") ?? "",
                        String(mo, "DriverVersion") ?? "",
                        String(mo, "Manufacturer") ?? "",
                        String(mo, "IsSigned") ?? "",
                        String(mo, "Signer") ?? "",
                        String(mo, "InfName") ?? ""));
                }
            }
            foreach (var mo in Query("SELECT Name, PathName, State FROM Win32_SystemDriver"))
            {
                using (mo)
                {
                    list.Add(new DriverInfo(
                        "System",
                        String(mo, "Name") ?? "",
                        "",
                        String(mo, "PathName") ?? "",
                        "",
                        "",
                        String(mo, "State") ?? ""));
                }
            }
            return Fact<IReadOnlyList<DriverInfo>>.Ok(list);
        }
        catch (Exception)
        {
            return Fact<IReadOnlyList<DriverInfo>>.Unavailable();
        }
    }

    private static IEnumerable<ManagementObject> QueryCim(string wql)
    {
        var scope = new ManagementScope(CimScope);
        scope.Connect();
        using var searcher = new ManagementObjectSearcher(scope, new ObjectQuery(wql));
        using var results = searcher.Get();
        foreach (ManagementObject mo in results)
            yield return mo;
    }
}
