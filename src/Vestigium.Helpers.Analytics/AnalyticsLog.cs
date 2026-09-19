using Vestigium.Logging;

namespace Vestigium.Helpers.Analytics;

/// <summary>
/// Package-local writes into Vestigium.Logging. No-ops when the host has not
/// initialized. This is not HelperLog and is not part of the public API.
/// <see cref="AnalyticsCatalog.AppId"/> stamps the JSON APPID field (library identity).
/// The log folder follows the host process APPID.
/// </summary>
internal static class AnalyticsLog
{
    internal const int MaxNameLength = 64;

    public static string NewId() => Guid.NewGuid().ToString("N")[..8];

    /// <summary>
    /// Label safe for a log property and for <see cref="NumericSeries.Name"/>.
    /// Strips C0 controls (including CR/LF/TAB), trims, truncates to <see cref="MaxNameLength"/>.
    /// </summary>
    public static string? SanitizeName(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return null;

        var chars = new char[name.Length];
        var n = 0;
        var lastSpace = false;
        foreach (var c in name)
        {
            if (c <= 32)
            {
                if (n == 0 || lastSpace)
                    continue;
                chars[n++] = ' ';
                lastSpace = true;
                continue;
            }

            chars[n++] = c;
            lastSpace = false;
        }

        while (n > 0 && chars[n - 1] == ' ')
            n--;
        if (n == 0)
            return null;
        if (n > MaxNameLength)
            n = MaxNameLength;
        return new string(chars, 0, n).TrimEnd();
    }

    public static IReadOnlyDictionary<string, string?> Props(params (string Key, string? Value)[] pairs)
    {
        var map = new Dictionary<string, string?>(pairs.Length, StringComparer.Ordinal);
        foreach (var (key, value) in pairs)
            map[key] = string.Equals(key, "name", StringComparison.Ordinal) ? SanitizeName(value) : value;
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
            appId: AnalyticsCatalog.AppId,
            correlationId: correlationId,
            properties: properties);
    }
}
