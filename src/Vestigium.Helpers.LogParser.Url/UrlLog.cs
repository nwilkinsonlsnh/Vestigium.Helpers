using Vestigium.Logging;

namespace Vestigium.Helpers.LogParser.Url;

internal static class UrlLog
{
    public static void Information(int eventId, string message)
        => Write(eventId, VestigiumLogLevel.Information, VestigiumStatus.Success, message);

    public static void Error(int eventId, string message)
        => Write(eventId, VestigiumLogLevel.Error, VestigiumStatus.Failed, message);

    private static void Write(int eventId, VestigiumLogLevel level, VestigiumStatus status, string message)
    {
        if (!VestigiumLogger.IsInitialized)
            return;

        VestigiumLog.Write(
            eventId,
            level,
            status,
            UrlCatalog.Category,
            UrlCatalog.Subcategories.Scan,
            message,
            appId: UrlCatalog.AppId);
    }
}
