using System.Runtime.Versioning;
using Vestigium.Helpers;
using Vestigium.Logging;

namespace Vestigium.Helpers.WinReg;

/// <summary>Windows Registry helpers. Windows-only.</summary>
[SupportedOSPlatform("windows")]
public static class RegistryHelper
{
    public static string Identity => "Vestigium.Helpers.WinReg";

    public static TimeSpan ConnectTimeout { get; set; } = TimeSpan.FromSeconds(3);

    public static RegistryClient Local { get; } = new(null);

    public static RegistryClient For(string machine)
        => new(HelperGuard.NotBlank(machine, nameof(machine)));

    public static bool CanConnect(string machine)
        => CanConnect(machine, out _);

    public static bool CanConnect(string machine, out string? reason)
    {
        var client = For(machine);
        if (client.IsLocal)
        {
            reason = null;
            return true;
        }

        try
        {
            var timeout = ConnectTimeout <= TimeSpan.Zero ? TimeSpan.FromSeconds(3) : ConnectTimeout;
            var task = Task.Run(() => client.Open(
                RegistryHiveKind.LocalMachine,
                string.Empty,
                RegistryViewKind.Default,
                writable: false));
            if (!task.Wait(timeout))
            {
                reason = "timeout";
                return false;
            }

            using var key = task.Result;
            if (key is null)
            {
                reason = "OpenRemoteBaseKey failed";
                return false;
            }

            reason = null;
            return true;
        }
        catch (Exception ex)
        {
            reason = ex.GetType().Name;
            return false;
        }
    }

    public static string Probe()
    {
        var app = HelperLog.AppIds.WinReg;
        HelperLog.Information(app, VestigiumStatus.Pending, HelperLog.Subcategories.Inventory, "Opening HKCU\\Software.");
        _ = Local.GetKey(RegistryHiveKind.CurrentUser, "Software", RegistryViewKind.Default, RegistryDetailLevel.Identity);
        HelperLog.Information(app, VestigiumStatus.Success, HelperLog.Subcategories.Inventory, "Registry probe complete. Identity=" + Identity);
        return Identity;
    }
}
