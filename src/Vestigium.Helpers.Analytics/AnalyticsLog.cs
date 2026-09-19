using Vestigium.Logging;

namespace Vestigium.Helpers.Analytics;

/// <summary>
/// Package-local writes into Vestigium.Logging. No-ops when the host has not
/// initialized. This is not HelperLog and is not part of the public API.
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
    {
        if (!VestigiumLogger.IsInitialized)
            return;
        VestigiumLog.Debug(eventId, status, AnalyticsCatalog.Category, subcategory, message,
            correlationId, properties, AnalyticsCatalog.AppId);
    }

    public static void Information(int eventId, VestigiumStatus status, string subcategory, string message,
        string? correlationId = null, IReadOnlyDictionary<string, string?>? properties = null)
    {
        if (!VestigiumLogger.IsInitialized)
            return;
        VestigiumLog.Information(eventId, status, AnalyticsCatalog.Category, subcategory, message,
            correlationId, properties, AnalyticsCatalog.AppId);
    }

    public static void Warning(int eventId, VestigiumStatus status, string subcategory, string message,
        string? correlationId = null, IReadOnlyDictionary<string, string?>? properties = null)
    {
        if (!VestigiumLogger.IsInitialized)
            return;
        VestigiumLog.Warning(eventId, status, AnalyticsCatalog.Category, subcategory, message,
            correlationId: correlationId, properties: properties, appId: AnalyticsCatalog.AppId);
    }

    public static void Error(int eventId, VestigiumStatus status, string subcategory, string message,
        Exception? exception = null, string? correlationId = null, IReadOnlyDictionary<string, string?>? properties = null)
    {
        if (!VestigiumLogger.IsInitialized)
            return;
        VestigiumLog.Error(eventId, status, AnalyticsCatalog.Category, subcategory, message,
            exception, correlationId, properties, AnalyticsCatalog.AppId);
    }

    public static void Unexpected(int eventId, string subcategory, Exception exception, string? correlationId = null)
    {
        if (exception is ArgumentException or ArgumentOutOfRangeException or InvalidOperationException)
            return;
        Error(eventId, VestigiumStatus.Failed, subcategory, "unexpected failure", exception, correlationId);
    }
}
