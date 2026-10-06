using Vestigium.Logging;

namespace Vestigium.Helpers.LogParser.Har;

internal static class HarLog
{
    public static void Information(int eventId, string message)
        => Write(eventId, VestigiumLogLevel.Information, VestigiumStatus.Success, message, null);

    public static void Error(int eventId, string message)
        => Write(eventId, VestigiumLogLevel.Error, VestigiumStatus.Failed, message, null);

    private static void Write(
        int eventId,
        VestigiumLogLevel level,
        VestigiumStatus status,
        string message,
        Exception? exception)
    {
        if (!VestigiumLogger.IsInitialized)
            return;

        VestigiumLog.Write(
            eventId,
            level,
            status,
            HarCatalog.Category,
            HarCatalog.Subcategories.Parse,
            message,
            exception: exception,
            appId: HarCatalog.AppId);
    }
}
