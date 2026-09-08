using Vestigium.Helpers;
using Vestigium.Logging;

namespace Vestigium.Helpers.Hashing;

/// <summary>
/// String and file hashing helpers. Skeleton only — do not grow the API until the Hashing SRS is accepted.
/// Libraries never call Initialize. Probe writes through HelperLog, a no-op until the host starts logging.
/// </summary>
public static class HashingHelper
{
    public static string Identity => "Vestigium.Helpers.Hashing";

    public static string Probe()
    {
        var app = HelperLog.AppIds.Hashing;
        HelperLog.Information(app, VestigiumStatus.Pending, app, "Opening a hashing helper probe.");
        HelperLog.Information(app, VestigiumStatus.Success, app, "Hashing probe complete. Identity=" + Identity);
        return Identity;
    }
}
