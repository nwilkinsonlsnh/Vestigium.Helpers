using Vestigium.Logging;

namespace Vestigium.Helpers.Hashing;

/// <summary>
/// ALCOA+ logs for Hashing. Never original input, HMAC keys, passwords, salts,
/// or PHC verifiers — even at Debug. File digests may be logged; they are the audit record.
/// Failed never attaches Exception.ToString().
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
        if (string.Equals(verb, "Probe", StringComparison.Ordinal))
        {
            Write(HashingEvents.ProbeComplete, VestigiumLogLevel.Information, VestigiumStatus.Success, "probe complete", verb, detail);
            return;
        }

        Write(HashingEvents.OperationComplete, VestigiumLogLevel.Information, VestigiumStatus.Success, "operation complete", verb, detail);
    }

    public static void Failed(string message)
        => Write(HashingEvents.OperationFailed, VestigiumLogLevel.Error, VestigiumStatus.Failed, "operation failed", "Failed", message);

    public static IDisposable Begin(string method, string detail)
    {
        Pending(method, detail);
        return NullScope.Instance;
    }

    public static string RequireNotBlank(string? value, string paramName)
    {
        if (!string.IsNullOrWhiteSpace(value))
            return value.Trim();
        Write(HashingEvents.OperationFailed, VestigiumLogLevel.Error, VestigiumStatus.Failed, "operation failed", "Rejected", paramName);
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
