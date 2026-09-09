using Vestigium.Helpers;
using Vestigium.Logging;

namespace Vestigium.Helpers.Encryption;

/// <summary>
/// ALCOA+ logs for Encryption. Attributable (APPID + actor + key prefix), contemporaneous
/// (logger timestamp), complete (Pending then Success/Failed, Warning on token override),
/// never original secret material — even at Debug.
/// </summary>
internal static class EncryptionLog
{
    private const string App = HelperLog.AppIds.Encryption;
    private const string EncryptionSub = "Encryption";
    private const string TokenSub = "Token";

    public static void Pending(string verb, string detail)
        => HelperLog.Information(App, VestigiumStatus.Pending, EncryptionSub, Safe($"{verb}: {detail}"));

    public static void Success(string verb, string detail)
        => HelperLog.Information(App, VestigiumStatus.Success, EncryptionSub, Safe($"{verb}: {detail}"));

    public static void Failed(string message)
        => HelperLog.Error(App, VestigiumStatus.Failed, EncryptionSub, Safe(message));

    public static void TokenWarning(string verb, string detail)
        => HelperLog.Warning(App, VestigiumStatus.Success, TokenSub, Safe($"{verb}: {detail}"));

    public static IDisposable Begin(string method, string detail)
        => HelperLog.Begin(App, EncryptionSub, method, Safe(detail));

    public static string Safe(string message)
    {
        if (string.IsNullOrEmpty(message))
            return message;
        if (EncryptionAudit.LooksLikeSecret(message))
            return "[redacted]";
        return message;
    }
}

internal static class EncryptionAudit
{
    public const int ActorMax = 50;
    public const int ReasonMax = 80;

    public static string TokenLabel(EncryptionKeyRecord row)
        => $"key={Prefix(row.ThumbprintSha256)}";

    public static string Prefix(string thumbHex)
    {
        if (string.IsNullOrWhiteSpace(thumbHex))
            return "????????";
        thumbHex = thumbHex.Trim().ToLowerInvariant();
        return thumbHex.Length <= 8 ? thumbHex : thumbHex[..8];
    }

    public static string Actor(string requestedBy)
    {
        var value = EncryptionKeyRecord.Clamp(requestedBy, ActorMax, nameof(requestedBy));
        if (LooksLikeSecret(value))
            throw new ArgumentException("RequestedBy must not contain key material.", nameof(requestedBy));
        return value;
    }

    public static string Reason(string reason)
    {
        var value = EncryptionKeyRecord.Clamp(reason, ReasonMax, nameof(reason));
        if (LooksLikeSecret(value))
            throw new ArgumentException("Override reason must not contain key material.", nameof(reason));
        return value;
    }

    public static bool LooksLikeSecret(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return false;
        if (value.Contains("BEGIN ", StringComparison.OrdinalIgnoreCase)
            || value.Contains("PRIVATE KEY", StringComparison.OrdinalIgnoreCase)
            || value.Contains("PKCS8", StringComparison.OrdinalIgnoreCase)
            || value.Contains("-----"))
            return true;

        var compact = value.Replace(" ", "", StringComparison.Ordinal).Replace("\n", "", StringComparison.Ordinal);
        if (compact.Length >= 32 && compact.All(Uri.IsHexDigit))
            return true;
        if (compact.Length >= 44 && compact.All(IsBase64Char))
            return true;
        return false;
    }

    private static bool IsBase64Char(char c)
        => char.IsLetterOrDigit(c) || c is '+' or '/' or '=' or '-' or '_';
}
