using System.Runtime.InteropServices;
using System.Security.Principal;

namespace Vestigium.Helpers.Network.RawSocket;

public static class RawSocketRights
{
    public const int CapNetRaw = 13;

    public static RawSocketAccess Check()
    {
        if (OperatingSystem.IsWindows())
            return CheckWindows();
        if (OperatingSystem.IsLinux())
            return CheckLinux();
        return new RawSocketAccess(RawSocketPrivilege.Denied, "unsupported");
    }

    private static RawSocketAccess CheckWindows()
    {
        try
        {
            using var identity = WindowsIdentity.GetCurrent();
            var principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator)
                ? new RawSocketAccess(RawSocketPrivilege.Granted, "administrator")
                : new RawSocketAccess(RawSocketPrivilege.Denied, "not-administrator");
        }
        catch (Exception ex)
        {
            return new RawSocketAccess(RawSocketPrivilege.Denied, ex.GetType().Name);
        }
    }

    private static RawSocketAccess CheckLinux()
    {
        try
        {
            if (HasCap(CapNetRaw))
                return new RawSocketAccess(RawSocketPrivilege.Granted, "cap-net-raw");
            return new RawSocketAccess(RawSocketPrivilege.Denied, "missing-cap-net-raw");
        }
        catch (Exception ex)
        {
            return new RawSocketAccess(RawSocketPrivilege.Denied, ex.GetType().Name);
        }
    }

    private static bool HasCap(int bit)
    {
        if (!File.Exists("/proc/self/status"))
            return false;

        foreach (var line in File.ReadLines("/proc/self/status"))
        {
            if (!line.StartsWith("CapEff:", StringComparison.Ordinal))
                continue;
            var hex = line["CapEff:".Length..].Trim();
            if (!ulong.TryParse(hex, System.Globalization.NumberStyles.HexNumber, null, out var mask))
                return false;
            return (mask & (1UL << bit)) != 0;
        }

        return false;
    }
}
