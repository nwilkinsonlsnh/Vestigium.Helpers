using Vestigium.Helpers;
using Vestigium.Logging;
namespace Vestigium.Helpers.Encryption;

/// <summary>
/// Hashing, symmetric encryption, and secret-handling helpers.
/// </summary>
public static class EncryptionHelper
{
    public static string Identity => "Vestigium.Helpers.Encryption";

    public static string Probe()
    {
        var app = HelperLog.AppIds.Encryption;
        HelperLog.Information(app, VestigiumStatus.Pending, app, "Hashing demo payload.");
        HelperLog.Information(app, VestigiumStatus.Success, app, "Hash probe complete. Identity=" + Identity);
        return Identity;
    }
}
