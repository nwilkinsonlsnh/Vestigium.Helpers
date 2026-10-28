using Vestigium.Logging;

namespace Vestigium.Helpers.Charts;

internal static class ChartsLog
{
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
        VestigiumLog.Debug(eventId, status, ChartsCatalog.Category, subcategory, message,
            correlationId, properties, ChartsCatalog.AppId);
    }

    public static void Information(int eventId, VestigiumStatus status, string subcategory, string message,
        string? correlationId = null, IReadOnlyDictionary<string, string?>? properties = null)
    {
        if (!VestigiumLogger.IsInitialized)
            return;
        VestigiumLog.Information(eventId, status, ChartsCatalog.Category, subcategory, message,
            correlationId, properties, ChartsCatalog.AppId);
    }

    public static void Error(int eventId, VestigiumStatus status, string subcategory, string message,
        Exception? exception = null, string? correlationId = null, IReadOnlyDictionary<string, string?>? properties = null)
    {
        if (!VestigiumLogger.IsInitialized)
            return;
        VestigiumLog.Error(eventId, status, ChartsCatalog.Category, subcategory, message,
            exception, correlationId, properties, ChartsCatalog.AppId);
    }

    public static void Unexpected(Exception exception, string? correlationId = null)
    {
        if (exception is ArgumentException or ArgumentOutOfRangeException or ArgumentNullException or InvalidOperationException)
            return;
        Error(ChartsEvents.ChartThrown, VestigiumStatus.Failed, ChartsCatalog.Subcategories.Chart,
            "unexpected failure", exception, correlationId);
    }
}
