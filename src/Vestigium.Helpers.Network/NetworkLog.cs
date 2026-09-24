using Vestigium.Logging;

namespace Vestigium.Helpers.Network;

/// <summary>
/// Only logging door for this library. Category Helpers, APPID Network.
/// Hosts initialize VestigiumLogger. Never packet payloads, WLAN keys, or Exception objects.
/// Absolute paths are reduced to a file name on every line.
/// </summary>
internal static class NetworkLog
{
    internal const string App = HelperLog.AppIds.Network;

    public static IDisposable Begin(string subcategory, string method, string? detail = null, string? correlationId = null)
        => HelperLog.Begin(App, subcategory, method, Redact(detail), correlationId);

    public static void Pending(string subcategory, string message)
        => HelperLog.Information(App, VestigiumStatus.Pending, subcategory, Redact(message));

    public static void Success(string subcategory, string message)
        => HelperLog.Information(App, VestigiumStatus.Success, subcategory, Redact(message));

    public static void Warning(string subcategory, string message)
        => HelperLog.Warning(App, VestigiumStatus.Warning, subcategory, Redact(message));

    public static void Failed(string subcategory, string message)
        => HelperLog.Error(App, VestigiumStatus.Failed, subcategory, Redact(message));

    public static void Reject(string subcategory, string method, string message)
        => HelperLog.Reject(App, subcategory, method, Redact(message));

    public static void RouteDenied(string method, string message)
        => HelperLog.WriteEvent(
            NetworkEvents.RouteDenied,
            VestigiumLogLevel.Error,
            VestigiumStatus.Failed,
            HelperLog.Subcategories.Route,
            HelperLog.Line("reject", method, Redact(message), HelperLog.CorrelationId));

    public static void IcmpForbidden(string message)
        => HelperLog.WriteEvent(
            NetworkEvents.IcmpForbidden,
            VestigiumLogLevel.Error,
            VestigiumStatus.Failed,
            HelperLog.Subcategories.Icmp,
            Redact(message));

    public static void WindowMissed(string message)
        => HelperLog.WriteEvent(
            NetworkEvents.CampaignWindowMissed,
            VestigiumLogLevel.Warning,
            VestigiumStatus.Warning,
            HelperLog.Subcategories.Campaign,
            Redact(message));

    internal static string? Redact(string? message)
    {
        if (string.IsNullOrEmpty(message))
            return message;

        var parts = message.Split(' ');
        for (var i = 0; i < parts.Length; i++)
            parts[i] = RedactToken(parts[i]);
        return string.Join(' ', parts);
    }

    internal static string FileName(string path)
        => FileNameOnly(path);

    private static string RedactToken(string token)
    {
        var eq = token.IndexOf('=');
        if (eq <= 0 || eq >= token.Length - 1) return LooksLikePath(token) ? FileNameOnly(token) : token;
        var key = token[..(eq + 1)];
        var value = token[(eq + 1)..];
        return LooksLikePath(value) ? key + FileNameOnly(value) : token;

    }

    internal static bool LooksLikePath(string value)
    {
        if (string.IsNullOrEmpty(value) || value.Length < 3)
            return false;
        if (value.StartsWith(@"\\", StringComparison.Ordinal))
            return true;
        if (char.IsLetter(value[0]) && value[1] == ':' && (value[2] is '\\' or '/'))
            return true;
        return value[0] == '/' && value.IndexOf('/', 1) >= 0;
    }

    private static string FileNameOnly(string value)
    {
        try
        {
            var name = Path.GetFileName(value.TrimEnd('/', '\\'));
            return string.IsNullOrEmpty(name) ? value : name;
        }
        catch (ArgumentException)
        {
            return value;
        }
    }
}
