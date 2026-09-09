using Vestigium.Helpers;
using Vestigium.Logging;

namespace Vestigium.Helpers.Hashing;

/// <summary>
/// ALCOA+ logs for Hashing. Attributable (APPID Hashing), contemporaneous (logger timestamp),
/// complete (Pending then Success/Failed). Never original input, HMAC keys, passwords, salts,
/// or PHC verifiers — even at Debug. File digests may be logged; they are the audit record.
/// </summary>
internal static class HashingLog
{
    private const string App = HelperLog.AppIds.Hashing;
    private const string Sub = "Hashing";

    public static void Pending(string verb, string detail)
        => HelperLog.Information(App, VestigiumStatus.Pending, Sub, Safe($"{verb}: {detail}"));

    public static void Success(string verb, string detail)
        => HelperLog.Information(App, VestigiumStatus.Success, Sub, Safe($"{verb}: {detail}"));

    public static void Failed(string message)
        => HelperLog.Error(App, VestigiumStatus.Failed, Sub, Safe(message));

    public static IDisposable Begin(string method, string detail)
        => HelperLog.Begin(App, Sub, method, Safe(detail));

    public static string Safe(string message)
    {
        if (string.IsNullOrEmpty(message))
            return message;
        if (LooksLikeSecret(message))
            return "[redacted]";
        return message;
    }

    public static bool LooksLikeSecret(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return false;
        if (value.Contains("$argon2", StringComparison.OrdinalIgnoreCase))
            return true;
        if (value.Contains("BEGIN ", StringComparison.OrdinalIgnoreCase)
            || value.Contains("PRIVATE KEY", StringComparison.OrdinalIgnoreCase)
            || value.Contains("-----"))
            return true;
        if (value.Contains("password=", StringComparison.OrdinalIgnoreCase)
            || value.Contains("passphrase=", StringComparison.OrdinalIgnoreCase)
            || value.Contains("hmac-key=", StringComparison.OrdinalIgnoreCase)
            || value.Contains("salt=", StringComparison.OrdinalIgnoreCase))
            return true;
        return false;
    }
}
