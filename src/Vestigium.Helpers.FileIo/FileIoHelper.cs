using Vestigium.Helpers;
using Vestigium.Logging;
namespace Vestigium.Helpers.FileIo;

/// <summary>
/// File and directory I/O helpers with safe path handling.
/// </summary>
public static class FileIoHelper
{
    public static string Identity => "Vestigium.Helpers.FileIo";

    public static string Probe()
    {
        var app = HelperLog.AppIds.FileIo;
        HelperLog.Information(app, VestigiumStatus.Pending, app, "Resolving a demo path under %TEMP%.");
        HelperLog.Information(app, VestigiumStatus.Success, app, "FileIo probe complete. Identity=" + Identity);
        return Identity;
    }
}
