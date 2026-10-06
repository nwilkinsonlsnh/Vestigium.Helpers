using Vestigium.Logging;

namespace Vestigium.Helpers.LogParser;

internal static class LogParserLog
{
    public static void Error(
        int eventId,
        VestigiumStatus status,
        string subcategory,
        string message,
        Exception? exception = null,
        string? correlationId = null,
        IReadOnlyDictionary<string, string?>? properties = null)
        => Write(eventId, VestigiumLogLevel.Error, status, subcategory, message, exception, correlationId, properties);

    public static void Unexpected(int eventId, string subcategory, Exception exception, string? correlationId = null)
    {
        if (exception is ArgumentException or ArgumentOutOfRangeException or InvalidOperationException)
            return;
        Error(eventId, VestigiumStatus.Failed, subcategory, "unexpected failure", exception, correlationId);
    }

    private static void Write(
        int eventId,
        VestigiumLogLevel level,
        VestigiumStatus status,
        string subcategory,
        string message,
        Exception? exception,
        string? correlationId,
        IReadOnlyDictionary<string, string?>? properties)
    {
        if (!VestigiumLogger.IsInitialized)
            return;

        VestigiumLog.Write(
            eventId,
            level,
            status,
            LogParserCatalog.Category,
            subcategory,
            message,
            exception: exception,
            appId: LogParserCatalog.AppId,
            correlationId: correlationId,
            properties: properties);
    }
}
