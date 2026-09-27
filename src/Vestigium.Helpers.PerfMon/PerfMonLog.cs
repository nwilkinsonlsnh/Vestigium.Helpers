using Vestigium.Logging;

namespace Vestigium.Helpers.PerfMon;

/// <summary>
/// Package-local writes into Vestigium.Logging. No-ops when the host has not
/// initialized. Not part of the public API.
/// </summary>
internal static class PerfMonLog
{
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

    public static IReadOnlyDictionary<string, string?> Props(params (string Key, string? Value)[] pairs)
    {
        var map = new Dictionary<string, string?>(pairs.Length, StringComparer.Ordinal);
        foreach (var (key, value) in pairs)
            map[key] = value;
        return map;
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
            PerfMonCatalog.Category,
            subcategory,
            message,
            exception: exception,
            appId: PerfMonCatalog.AppId,
            correlationId: correlationId,
            properties: properties);
    }
}
