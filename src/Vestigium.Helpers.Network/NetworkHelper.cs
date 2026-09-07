using Vestigium.Helpers;
using Vestigium.Logging;
namespace Vestigium.Helpers.Network;

/// <summary>
/// HTTP and socket helper utilities for diagnostic hosts.
/// </summary>
public static class NetworkHelper
{
    public static string Identity => "Vestigium.Helpers.Network";

    public static string Probe()
    {
        var app = HelperLog.AppIds.Network;
        HelperLog.Information(app, VestigiumStatus.Pending, app, "Describing loopback diagnostic endpoint.");
        HelperLog.Information(app, VestigiumStatus.Success, app, "Network probe complete. Identity=" + Identity);
        return Identity;
    }
}
