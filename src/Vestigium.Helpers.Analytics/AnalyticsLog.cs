using Vestigium.Logging;

namespace Vestigium.Helpers.Analytics;

/// <summary>
/// Package-local writes into Vestigium.Logging. No-ops when the host has not
/// initialized. This is not HelperLog and is not part of the public API.
/// Writes inherit the host process APPID. Do not pass <see cref="AnalyticsCatalog.AppId"/>.
/// </summary>
internal static class AnalyticsLog
{
    public static string NewId() => Guid.NewGuid().ToString("N")[..8];

    public static IReadOnlyDictionary<string, string?> Props(params (string Key, string? Value)[] pairs)
    {
        var map = new Dictionary<string, string?>(pairs.Length, StringComparer.Ordinal);
        foreach (var (key, value) in pairs)
            map[key] = value;
        return map;
    }

    public static void Debug(int eventId, VestigiumStatus status, string subcategory, string message,
        string? correlationId = null, IReadOnlyDictionary<string, string?>? properties = null)
        => Write(eventId, VestigiumLogLevel.Debug, status, subcategory, message, null, correlationId, properties);

    public static void Information(int eventId, VestigiumStatus status, string subcategory, string message,
        string? correlationId = null, IReadOnlyDictionary<string, string?>? properties = null)
        => Write(eventId, VestigiumLogLevel.Information, status, subcategory, message, null, correlationId, properties);

    public static void Warning(int eventId, VestigiumStatus status, string subcategory, string message,
        string? correlationId = null, IReadOnlyDictionary<string, string?>? properties = null)
        => Write(eventId, VestigiumLogLevel.Warning, status, subcategory, message, null, correlationId, properties);

    public static void Error(int eventId, VestigiumStatus status, string subcategory, string message,
        Exception? exception = null, string? correlationId = null, IReadOnlyDictionary<string, string?>? properties = null)
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
            AnalyticsCatalog.Category,
            subcategory,
            message,
            exception: exception,
            correlationId: correlationId,
            properties: properties);
    }
}
