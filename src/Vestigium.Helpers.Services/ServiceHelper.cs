using Vestigium.Helpers;
using Vestigium.Logging;
namespace Vestigium.Helpers.Services;

/// <summary>
/// Windows Service and hosted-service control helpers.
/// </summary>
public static class ServiceHelper
{
    public static string Identity => "Vestigium.Helpers.Services";

    public static string Probe()
    {
        var app = HelperLog.AppIds.Services;
        HelperLog.Information(app, VestigiumStatus.Pending, app, "Enumerating service-control surface.");
        HelperLog.Information(app, VestigiumStatus.Success, app, "Service probe complete. Identity=" + Identity);
        return Identity;
    }
}
