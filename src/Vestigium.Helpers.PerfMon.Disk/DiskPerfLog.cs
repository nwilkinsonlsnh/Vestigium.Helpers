using Vestigium.Logging;

namespace Vestigium.Helpers.PerfMon.Disk;

internal static class DiskPerfLog
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
            DiskPerfCatalog.Category,
            subcategory,
            message,
            appId: DiskPerfCatalog.AppId,
            properties: properties);
    }
}
