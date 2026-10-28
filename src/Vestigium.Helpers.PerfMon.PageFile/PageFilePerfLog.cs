using Vestigium.Logging;

namespace Vestigium.Helpers.PerfMon.PageFile;

internal static class PageFilePerfLog
{
    public static void Debug(int eventId, VestigiumStatus status, string subcategory, string message,
        IReadOnlyDictionary<string, string?>? properties = null)
        => Write(eventId, VestigiumLogLevel.Debug, status, subcategory, message, properties);

    public static void Information(int eventId, VestigiumStatus status, string subcategory, string message,
        IReadOnlyDictionary<string, string?>? properties = null)
        => Write(eventId, VestigiumLogLevel.Information, status, subcategory, message, properties);

    public static void Warning(int eventId, VestigiumStatus status, string subcategory, string message,
        IReadOnlyDictionary<string, string?>? properties = null)
        => Write(eventId, VestigiumLogLevel.Warning, status, subcategory, message, properties);

    public static void Error(int eventId, VestigiumStatus status, string subcategory, string message,
        IReadOnlyDictionary<string, string?>? properties = null)
        => Write(eventId, VestigiumLogLevel.Error, status, subcategory, message, properties);

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
        IReadOnlyDictionary<string, string?>? properties)
    {
        if (!VestigiumLogger.IsInitialized)
            return;
        VestigiumLog.Write(
            eventId,
            level,
            status,
            PageFilePerfCatalog.Category,
            subcategory,
            message,
            appId: PageFilePerfCatalog.AppId,
            properties: properties);
    }
}
