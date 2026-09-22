using Vestigium.Logging;

namespace Vestigium.Helpers.Hashing;

/// <summary>
/// ALCOA+ logs for Hashing. Never original input, HMAC keys, passwords, salts,
/// or PHC verifiers — even at Debug. File digests may be logged; they are the audit record.
/// String / bytes hashes never log digest hex. Failed never attaches Exception.ToString().
/// </summary>
internal static class HashingLog
{
    public static IReadOnlyDictionary<string, string?> Props(params (string Key, string? Value)[] pairs)
    {
        var map = new Dictionary<string, string?>(pairs.Length, StringComparer.Ordinal);
        foreach (var (key, value) in pairs)
            map[key] = Safe(value);
        return map;
    }

    public static void Pending(string verb, string detail)
    {
        if (string.Equals(verb, "Probe", StringComparison.Ordinal))
        {
            Write(HashingEvents.ProbeEnter, VestigiumLogLevel.Debug, VestigiumStatus.Pending, "enter Probe", verb, detail);
            return;
        }

        Write(HashingEvents.OperationEnter, VestigiumLogLevel.Debug, VestigiumStatus.Pending, "enter operation", verb, detail);
    }

    public static void Success(string verb, string detail)
    {
        var (eventId, message) = CompleteOf(verb);
        Write(eventId, VestigiumLogLevel.Information, VestigiumStatus.Success, message, verb, detail);
    }

    public static void Failed(string message)
        => Write(HashingEvents.OperationFailed, VestigiumLogLevel.Error, VestigiumStatus.Failed, "operation failed", "Failed", message);

    public static void HashComplete(string detail) => Success("HashString", detail);
    public static void HashFileComplete(string detail) => Success("HashFile", detail);
    public static void HmacComplete(string detail) => Success("HmacString", detail);
    public static void KmacComplete(string detail) => Success("KmacString", detail);
    public static void ShakeComplete(string detail) => Success("ShakeString", detail);
    public static void ChecksumComplete(string detail) => Success("ChecksumString", detail);
    public static void PasswordHashed(string detail) => Success("HashPassword", detail);
    public static void PasswordVerify(string detail) => Success("VerifyPassword", detail);
    public static void ConvertComplete(string detail) => Success("Convert", detail);
    public static void Rejected(string detail)
        => Write(HashingEvents.Rejected, VestigiumLogLevel.Error, VestigiumStatus.Failed, "input rejected", "Rejected", detail);

    public static IDisposable Begin(string method, string detail)
    {
        Pending(method, detail);
        return NullScope.Instance;
    }

    public static string RequireNotBlank(string? value, string paramName)
    {
        if (!string.IsNullOrWhiteSpace(value))
            return value.Trim();
        Rejected(paramName);
        throw new ArgumentException("Value is required.", paramName);
    }

    public static string Safe(string? message)
    {
        if (string.IsNullOrEmpty(message))
            return message ?? string.Empty;
        return LooksLikeSecret(message) ? "[redacted]" : message;
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

    private static (int EventId, string Message) CompleteOf(string verb)
    {
        if (string.Equals(verb, "Probe", StringComparison.Ordinal))
            return (HashingEvents.ProbeComplete, "probe complete");
        if (verb.StartsWith("HashFile", StringComparison.Ordinal))
            return (HashingEvents.HashFileComplete, "file hash complete");
        if (verb.StartsWith("Hash", StringComparison.Ordinal) && !verb.StartsWith("HashPassword", StringComparison.Ordinal))
            return (HashingEvents.HashComplete, "string or bytes hash complete");
        if (verb.StartsWith("Hmac", StringComparison.Ordinal) || verb.StartsWith("VerifyHmac", StringComparison.Ordinal))
            return (HashingEvents.HmacComplete, "HMAC complete");
        if (verb.StartsWith("Kmac", StringComparison.Ordinal) || verb.StartsWith("VerifyKmac", StringComparison.Ordinal))
            return (HashingEvents.KmacComplete, "KMAC complete");
        if (verb.StartsWith("Shake", StringComparison.Ordinal) || verb.StartsWith("VerifyShake", StringComparison.Ordinal))
            return (HashingEvents.ShakeComplete, "SHAKE complete");
        if (verb.StartsWith("Checksum", StringComparison.Ordinal) || verb.StartsWith("VerifyChecksum", StringComparison.Ordinal))
            return (HashingEvents.ChecksumComplete, "checksum complete");
        if (string.Equals(verb, "HashPassword", StringComparison.Ordinal))
            return (HashingEvents.PasswordHashed, "password hashed");
        if (string.Equals(verb, "VerifyPassword", StringComparison.Ordinal))
            return (HashingEvents.PasswordVerify, "password verify");
        if (verb.Contains("Convert", StringComparison.Ordinal))
            return (HashingEvents.ConvertComplete, "convert complete");
        return (HashingEvents.OperationComplete, "operation complete");
    }

    private static void Write(
        int eventId,
        VestigiumLogLevel level,
        VestigiumStatus status,
        string message,
        string verb,
        string detail)
    {
        if (!VestigiumLogger.IsInitialized)
            return;
        VestigiumLog.Write(
            eventId,
            level,
            status,
            HashingCatalog.Category,
            HashingCatalog.Subcategory,
            message,
            exception: null,
            correlationId: null,
            properties: Props(("verb", verb), ("detail", detail)),
            appId: HashingCatalog.AppId);
    }

    private sealed class NullScope : IDisposable
    {
        public static readonly NullScope Instance = new();
        public void Dispose() { }
    }
}

internal static class HelperGuard
{
    public static string NotBlank(string? value, string paramName)
        => HashingLog.RequireNotBlank(value, paramName);
}
