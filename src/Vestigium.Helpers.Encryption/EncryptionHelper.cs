using Vestigium.Helpers;
using Vestigium.Logging;

namespace Vestigium.Helpers.Encryption;

/// <summary>
/// Authenticated encryption for strings and files. Skeleton only — do not grow the API until the Encryption SRS is accepted.
/// Hashing lives in Vestigium.Helpers.Hashing. Libraries never call Initialize.
/// </summary>
public static class EncryptionHelper
{
    public static string Identity => "Vestigium.Helpers.Encryption";

    public static string Probe()
    {
        var app = HelperLog.AppIds.Encryption;
        HelperLog.Information(app, VestigiumStatus.Pending, app, "Opening an encryption helper probe.");
        HelperLog.Information(app, VestigiumStatus.Success, app, "Encryption probe complete. Identity=" + Identity);
        return Identity;
    }
}
