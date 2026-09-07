using Vestigium.Helpers;
using Vestigium.Logging;
namespace Vestigium.Helpers.Json;

/// <summary>
/// System.Text.Json helpers for file and stream payloads.
/// </summary>
public static class JsonHelper
{
    public static string Identity => "Vestigium.Helpers.Json";

    public static string Probe()
    {
        var app = HelperLog.AppIds.Json;
        HelperLog.Information(app, VestigiumStatus.Pending, app, "Serializing a demo payload.");
        HelperLog.Information(app, VestigiumStatus.Success, app, "JSON probe complete. Identity=" + Identity);
        return Identity;
    }
}
