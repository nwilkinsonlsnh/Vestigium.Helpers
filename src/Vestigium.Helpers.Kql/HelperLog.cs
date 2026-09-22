using Vestigium.Logging;

namespace Vestigium.Helpers.Kql;

/// <summary>
/// Kql-only door into Vestigium.Logging. Same call surface the existing files already use.
/// Writes are no-ops until the host initializes. Does not log filter text or row values.
/// </summary>
internal static class HelperLog
{
    public static class AppIds
    {
        public const string Kql = KqlLoggingCatalog.AppId;
    }

    public static class Subcategories
    {
        public const string Probe = "Probe";
        public const string Session = "Session";
        public const string Query = "Query";
    }

    public static void Information(string appId, VestigiumStatus status, string subcategory, string message)
        => Write(VestigiumLogLevel.Information, status, subcategory, message);

    public static void Warning(string appId, VestigiumStatus status, string subcategory, string message)
        => Write(VestigiumLogLevel.Warning, status, subcategory, message);

    private static void Write(VestigiumLogLevel level, VestigiumStatus status, string subcategory, string message)
    {
        if (!VestigiumLogger.IsInitialized)
            return;

        var eventId = EventId(level, subcategory, message);
        VestigiumLog.Write(
            eventId,
            level,
            status,
            KqlLoggingCatalog.Category,
            subcategory,
            CatalogMessage(eventId),
            exception: null,
            correlationId: null,
            properties: new Dictionary<string, string?>(StringComparer.Ordinal)
            {
                ["detail"] = message
            },
            appId: KqlLoggingCatalog.AppId);
    }

    private static int EventId(VestigiumLogLevel level, string subcategory, string message)
    {
        if (string.Equals(subcategory, AppIds.Kql, StringComparison.Ordinal)
            || string.Equals(subcategory, Subcategories.Probe, StringComparison.Ordinal))
        {
            if (message.Contains("probe complete", StringComparison.OrdinalIgnoreCase))
                return KqlEvents.ProbeComplete;
            return KqlEvents.ProbeEnter;
        }

        if (string.Equals(subcategory, Subcategories.Session, StringComparison.Ordinal))
            return KqlEvents.SessionCreated;

        if (level == VestigiumLogLevel.Warning && statusIsFailed(message))
            return KqlEvents.QueryFailed;

        return KqlEvents.QueryWarning;
    }

    private static bool statusIsFailed(string message)
        => message.Contains("failed", StringComparison.OrdinalIgnoreCase);

    private static string CatalogMessage(int eventId) => eventId switch
    {
        KqlEvents.ProbeEnter => "enter Probe",
        KqlEvents.ProbeComplete => "probe complete",
        KqlEvents.SessionCreated => "session created",
        KqlEvents.QueryWarning => "query warning",
        KqlEvents.QueryFailed => "query failed",
        _ => "query warning"
    };
}
