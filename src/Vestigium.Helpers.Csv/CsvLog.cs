using Vestigium.Logging;

namespace Vestigium.Helpers.Csv;

internal static class CsvLog
{
    public static string NewId() => Guid.NewGuid().ToString("N")[..8];

    public static IReadOnlyDictionary<string, string?> Props(params (string Key, string? Value)[] pairs)
    {
        var map = new Dictionary<string, string?>(pairs.Length, StringComparer.Ordinal);
        foreach (var (key, value) in pairs)
            map[key] = value;
        return map;
    }

    public static void Debug(int eventId, string subcategory, string message, string? correlationId = null,
        IReadOnlyDictionary<string, string?>? properties = null, string? appId = null)
    {
        if (!VestigiumLogger.IsInitialized)
            return;
        VestigiumLog.Debug(eventId, VestigiumStatus.Pending, CsvCatalog.Category, subcategory, message,
            correlationId, properties, appId ?? CsvCatalog.AppId);
    }

    public static void Information(int eventId, string subcategory, string message, string? correlationId = null,
        IReadOnlyDictionary<string, string?>? properties = null, string? appId = null)
    {
        if (!VestigiumLogger.IsInitialized)
            return;
        VestigiumLog.Information(eventId, VestigiumStatus.Success, CsvCatalog.Category, subcategory, message,
            correlationId, properties, appId ?? CsvCatalog.AppId);
    }

    public static void Warning(int eventId, string subcategory, string message, string? correlationId = null,
        IReadOnlyDictionary<string, string?>? properties = null, string? appId = null)
    {
        if (!VestigiumLogger.IsInitialized)
            return;
        VestigiumLog.Warning(eventId, VestigiumStatus.Warning, CsvCatalog.Category, subcategory, message,
            correlationId: correlationId, properties: properties, appId: appId ?? CsvCatalog.AppId);
    }

    public static void Error(int eventId, string subcategory, string message, Exception? exception = null,
        string? correlationId = null, IReadOnlyDictionary<string, string?>? properties = null, string? appId = null)
    {
        if (!VestigiumLogger.IsInitialized)
            return;
        VestigiumLog.Error(eventId, VestigiumStatus.Failed, CsvCatalog.Category, subcategory, message,
            exception, correlationId, properties, appId ?? CsvCatalog.AppId);
    }

    public static void Unexpected(int eventId, string subcategory, Exception exception, string? correlationId = null, string? appId = null)
    {
        if (exception is ArgumentException or ArgumentOutOfRangeException or ArgumentNullException
            or InvalidOperationException or FileNotFoundException or ObjectDisposedException or CsvFormatException)
            return;
        Error(eventId, subcategory, "unexpected failure", exception, correlationId, appId: appId);
    }

    public static string RequireNotBlank(string? value, string paramName)
    {
        if (!string.IsNullOrWhiteSpace(value))
            return value.Trim();
        Error(CsvEvents.SessionRejected, CsvCatalog.Subcategories.Session, "rejected session",
            properties: Props(("param", paramName), ("reason", "blank")));
        throw new ArgumentException("Value is required.", paramName);
    }

    public static string RequireFile(string? path, string paramName)
    {
        var target = RequireNotBlank(path, paramName);
        if (File.Exists(target))
            return target;
        Error(CsvEvents.SessionRejected, CsvCatalog.Subcategories.Session, "rejected session",
            properties: Props(("path", target), ("reason", "missing-file")));
        throw new FileNotFoundException($"File not found: {target}", target);
    }

    public static void ThrowIfDisposed(bool disposed, object instance)
    {
        if (!disposed)
            return;
        Error(CsvEvents.SessionRejected, CsvCatalog.Subcategories.Session, "rejected session",
            properties: Props(("reason", "disposed")));
        throw new ObjectDisposedException(instance.GetType().Name);
    }
}
