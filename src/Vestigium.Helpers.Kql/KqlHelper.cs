using Vestigium.Helpers;
using Vestigium.Logging;

namespace Vestigium.Helpers.Kql;

/// <summary>KQL-inspired filter helpers. Phase 0: Identity and Probe only.</summary>
public static class KqlHelper
{
    public static string Identity => "Vestigium.Helpers.Kql";

    public static string Probe()
    {
        var app = HelperLog.AppIds.Kql;
        HelperLog.Information(app, VestigiumStatus.Pending, app, "Describing the KQL filter catalog.");
        HelperLog.Information(app, VestigiumStatus.Success, app, "KQL probe complete. Identity=" + Identity);
        return Identity;
    }
}
