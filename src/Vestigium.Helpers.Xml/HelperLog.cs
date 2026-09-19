using Vestigium.Logging;

namespace Vestigium.Helpers.Xml;

/// <summary>
/// Xml-only door into Vestigium.Logging. No payload dumps. No-op until host Initialize.
/// </summary>
internal static class HelperLog
{
    public const string Category = XmlCatalog.Category;

    public static class AppIds
    {
        public const string Xml = XmlCatalog.AppId;
    }

    public static class Subcategories
    {
        public const string Probe = "Probe";
        public const string Guard = "Guard";
        public const string Document = "Document";
        public const string Session = "Session";
        public const string Multi = "Multi";
        public const string Save = "Save";
        public const string Query = "Query";
        public const string Safety = "Safety";
        public const string Snapshot = "Snapshot";
        public const string Diff = "Diff";
        public const string Commit = "Commit";
    }

    private static readonly AsyncLocal<ScopeState?> Scope = new();
    public static string CurrentAppId => Scope.Value?.AppId ?? AppIds.Xml;
    public static string CurrentMethod => Scope.Value?.Method ?? Subcategories.Guard;
    public static string CurrentSubcategory => Scope.Value?.Subcategory ?? Subcategories.Guard;
    public static string? CorrelationId => Scope.Value?.CorrelationId;

    public static string NewId() => Guid.NewGuid().ToString("N")[..8];

    public static IDisposable Begin(
        string appId,
        string subcategory,
        string method,
        string? detail = null,
        string? correlationId = null)
    {
        var parent = Scope.Value;
        var id = correlationId ?? parent?.CorrelationId;
        Scope.Value = new ScopeState(appId, subcategory, method, id, parent);
        Debug(appId, VestigiumStatus.Pending, subcategory, Line("enter", method, detail, id));
        return new PopScope(parent);
    }

    public static void Exit(string appId, string subcategory, string method, string? detail = null)
        => Debug(appId, VestigiumStatus.Success, subcategory, Line("exit", method, detail, CorrelationId));

    public static void Trap(Exception exception, string? detail = null)
        => Error(CurrentAppId, VestigiumStatus.Failed, CurrentSubcategory, Line("trap", CurrentMethod, detail ?? exception.GetType().Name, CorrelationId), exception);

    public static void Reject(string reason, Exception? exception = null)
        => Reject(CurrentAppId, CurrentSubcategory, CurrentMethod, reason, CorrelationId, exception);

    public static void Reject(
        string appId,
        string subcategory,
        string method,
        string reason,
        string? correlationId = null,
        Exception? exception = null)
        => Error(appId, VestigiumStatus.Failed, subcategory, Line("reject", method, reason, correlationId), exception);

    public static void Debug(string appId, VestigiumStatus status, string subcategory, string message, Exception? exception = null)
        => Write(appId, VestigiumLogLevel.Debug, status, subcategory, message, exception);

    public static void Information(string appId, VestigiumStatus status, string subcategory, string message, Exception? exception = null)
        => Write(appId, VestigiumLogLevel.Information, status, subcategory, message, exception);

    public static void Warning(string appId, VestigiumStatus status, string subcategory, string message, Exception? exception = null)
        => Write(appId, VestigiumLogLevel.Warning, status, subcategory, message, exception);

    public static void Error(string appId, VestigiumStatus status, string subcategory, string message, Exception? exception = null)
        => Write(appId, VestigiumLogLevel.Error, status, subcategory, message, exception);

    public static string Line(string verb, string method, string? detail, string? correlationId)
    {
        var message = string.IsNullOrWhiteSpace(detail) ? $"{verb} {method}" : $"{verb} {method} {detail}";
        if (string.IsNullOrWhiteSpace(correlationId))
            return message;
        return message + " id=" + correlationId;
    }

    private static void Write(
        string helperAppId,
        VestigiumLogLevel level,
        VestigiumStatus status,
        string subcategory,
        string message,
        Exception? exception)
    {
        if (!VestigiumLogger.IsInitialized)
            return;

        var eventId = EventId(level, subcategory, message);
        VestigiumLog.Write(
            eventId,
            level,
            Normalize(status),
            Category,
            subcategory,
            CatalogMessage(eventId),
            exception: null,
            correlationId: CorrelationId,
            properties: new Dictionary<string, string?>(StringComparer.Ordinal)
            {
                ["appId"] = helperAppId,
                ["detail"] = message,
                ["exceptionType"] = exception?.GetType().Name
            },
            appId: XmlCatalog.AppId);
    }

    private static VestigiumStatus Normalize(VestigiumStatus status)
        => status is VestigiumStatus.Success or VestigiumStatus.Warning or VestigiumStatus.Failed
            ? status
            : VestigiumStatus.Warning;

    private static int EventId(VestigiumLogLevel level, string subcategory, string message)
    {
        if (message.Contains("probe complete", StringComparison.OrdinalIgnoreCase)
            || message.Contains("XML probe complete", StringComparison.OrdinalIgnoreCase))
            return XmlEvents.ProbeComplete;
        if (string.Equals(subcategory, Subcategories.Probe, StringComparison.Ordinal))
            return level == VestigiumLogLevel.Information && message.Contains("complete", StringComparison.OrdinalIgnoreCase)
                ? XmlEvents.ProbeComplete
                : XmlEvents.ProbeEnter;

        return level switch
        {
            VestigiumLogLevel.Debug or VestigiumLogLevel.Verbose => XmlEvents.OperationEnter,
            VestigiumLogLevel.Warning => XmlEvents.OperationWarning,
            VestigiumLogLevel.Error or VestigiumLogLevel.Fatal => XmlEvents.OperationFailed,
            _ => XmlEvents.OperationComplete
        };
    }

    private static string CatalogMessage(int eventId) => eventId switch
    {
        XmlEvents.ProbeEnter => "enter Probe",
        XmlEvents.ProbeComplete => "probe complete",
        XmlEvents.OperationEnter => "enter operation",
        XmlEvents.OperationComplete => "operation complete",
        XmlEvents.OperationFailed => "operation failed",
        XmlEvents.OperationWarning => "operation warning",
        _ => "operation complete"
    };

    private sealed record ScopeState(
        string AppId, string Subcategory, string Method, string? CorrelationId, ScopeState? Parent);

    private sealed class PopScope : IDisposable
    {
        private readonly ScopeState? _parent;
        private int _done;
        public PopScope(ScopeState? parent) => _parent = parent;
        public void Dispose()
        {
            if (Interlocked.Exchange(ref _done, 1) == 1)
                return;
            Scope.Value = _parent;
        }
    }
}
