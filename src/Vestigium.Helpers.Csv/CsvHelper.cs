using Vestigium.Helpers;
using Vestigium.Logging;

namespace Vestigium.Helpers.Csv;

/// <summary>
/// CSV helpers. Skeleton only — do not grow the API until the Csv SRS is accepted.
/// Libraries never call Initialize. Probe writes through HelperLog, a no-op until the host starts logging.
/// </summary>
public static class CsvHelper
{
    public static string Identity => "Vestigium.Helpers.Csv";

    public static string Probe()
    {
        var app = HelperLog.AppIds.Csv;
        HelperLog.Information(app, VestigiumStatus.Pending, app, "Opening a CSV helper probe.");
        HelperLog.Information(app, VestigiumStatus.Success, app, "Csv probe complete. Identity=" + Identity);
        return Identity;
    }
}
