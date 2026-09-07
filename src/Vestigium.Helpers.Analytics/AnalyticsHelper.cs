using Vestigium.Helpers;
using Vestigium.Logging;
namespace Vestigium.Helpers.Analytics;

/// <summary>
/// Lightweight counters, timings, and metric snapshots.
/// </summary>
public static class AnalyticsHelper
{
    public static string Identity => "Vestigium.Helpers.Analytics";

    public static string Probe()
    {
        var app = HelperLog.AppIds.Analytics;
        HelperLog.Information(app, VestigiumStatus.Pending, app, "Opening an in-process counter snapshot.");
        HelperLog.Information(app, VestigiumStatus.Success, app, "Analytics probe complete. Identity=" + Identity);
        return Identity;
    }
}
