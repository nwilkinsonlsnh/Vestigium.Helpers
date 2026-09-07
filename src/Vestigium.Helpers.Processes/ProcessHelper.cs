using Vestigium.Helpers;
using Vestigium.Logging;
namespace Vestigium.Helpers.Processes;

/// <summary>
/// Process launch, capture, and lifetime helpers.
/// </summary>
public static class ProcessHelper
{
    public static string Identity => "Vestigium.Helpers.Processes";

    public static string Probe()
    {
        var app = HelperLog.AppIds.Processes;
        HelperLog.Information(app, VestigiumStatus.Pending, app, "Describing current process identity.");
        HelperLog.Information(app, VestigiumStatus.Success, app, "Process probe complete. Identity=" + Identity);
        return Identity;
    }
}
