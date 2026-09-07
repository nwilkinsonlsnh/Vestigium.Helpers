using System.Runtime.Versioning;
using Vestigium.Helpers;
using Vestigium.Logging;
namespace Vestigium.Helpers.WinReg;

/// <summary>
/// Windows Registry read/write helpers. Windows-only.
/// </summary>
[SupportedOSPlatform("windows")]
public static class RegistryHelper
{
    public static string Identity => "Vestigium.Helpers.WinReg";

    public static string Probe()
    {
        var app = HelperLog.AppIds.WinReg;
        HelperLog.Information(app, VestigiumStatus.Pending, app, "Opening HKCU\\Software\\Vestigium\\Helpers.Demo.");
        HelperLog.Information(app, VestigiumStatus.Success, app, "Registry probe complete. Identity=" + Identity);
        return Identity;
    }
}
