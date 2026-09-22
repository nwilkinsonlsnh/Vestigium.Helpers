using Vestigium.Logging;

namespace Vestigium.Helpers.Encryption;

/// <summary>
/// ALCOA+ logs for Encryption. Never writes original secret material — even at Debug.
/// Failed never takes an Exception (the engine would persist exception.ToString()).
/// </summary>
internal static class EncryptionLog
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
            Write(EncryptionEvents.ProbeEnter, VestigiumLogLevel.Debug, VestigiumStatus.Pending,
                EncryptionCatalog.Subcategories.Probe, "enter Probe", Props(("verb", verb), ("detail", detail)));
            return;
        }

        Write(EncryptionEvents.OperationEnter, VestigiumLogLevel.Debug, VestigiumStatus.Pending,
            EncryptionCatalog.Subcategories.Encryption, "enter operation", Props(("verb", verb), ("detail", detail)));
    }

    public static void Success(string verb, string detail)
    {
        if (string.Equals(verb, "Probe", StringComparison.Ordinal))
        {
            Write(EncryptionEvents.ProbeComplete, VestigiumLogLevel.Information, VestigiumStatus.Success,
                EncryptionCatalog.Subcategories.Probe, "probe complete", Props(("verb", verb), ("detail", detail)));
            return;
        }

        Write(EncryptionEvents.OperationComplete, VestigiumLogLevel.Information, VestigiumStatus.Success,
            EncryptionCatalog.Subcategories.Encryption, "operation complete", Props(("verb", verb), ("detail", detail)));
    }

    public static void Failed(string message)
    {
        var safe = Safe(message);
        if (safe.Contains("not found", StringComparison.OrdinalIgnoreCase))
        {
            Write(EncryptionEvents.FileRejected, VestigiumLogLevel.Error, VestigiumStatus.Failed,
                EncryptionCatalog.Subcategories.Encryption, "rejected file", Props(("reason", safe)));
            return;
        }

        Write(EncryptionEvents.OperationFailed, VestigiumLogLevel.Error, VestigiumStatus.Failed,
            EncryptionCatalog.Subcategories.Encryption, "operation failed", Props(("reason", safe)));
    }

    public static void TokenWarning(string verb, string detail)
        => Write(EncryptionEvents.TokenWarning, VestigiumLogLevel.Warning, VestigiumStatus.Warning,
            EncryptionCatalog.Subcategories.Token, "token warning", Props(("verb", verb), ("detail", detail)));

    public static IDisposable Begin(string method, string detail)
    {
        Pending(method, detail);
        return NullScope.Instance;
    }

    public static string RequireNotBlank(string? value, string paramName)
    {
        if (!string.IsNullOrWhiteSpace(value))
            return value.Trim();
        Write(EncryptionEvents.FileRejected, VestigiumLogLevel.Error, VestigiumStatus.Failed,
            EncryptionCatalog.Subcategories.Encryption, "rejected file", Props(("param", paramName), ("reason", "blank")));
        throw new ArgumentException("Value is required.", paramName);
    }

    public static string Safe(string? message)
    {
        if (string.IsNullOrEmpty(message))
            return message ?? string.Empty;
        if (EncryptionAudit.LooksLikeSecret(message))
            return "[redacted]";
        return message;
    }

    private static void Write(
        int eventId,
        VestigiumLogLevel level,
        VestigiumStatus status,
        string subcategory,
        string message,
        IReadOnlyDictionary<string, string?> properties)
    {
        if (!VestigiumLogger.IsInitialized)
            return;
        VestigiumLog.Write(eventId, level, status, EncryptionCatalog.Category, subcategory, message,
            exception: null, correlationId: null, properties: properties, appId: EncryptionCatalog.AppId);
    }

    private sealed class NullScope : IDisposable
    {
        public static readonly NullScope Instance = new();
        public void Dispose() { }
    }
}
