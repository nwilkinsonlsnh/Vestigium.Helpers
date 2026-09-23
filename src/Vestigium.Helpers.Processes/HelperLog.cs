using Vestigium.Logging;

namespace Vestigium.Helpers.Processes;

/// <summary>
/// Processes-only door into Vestigium.Logging. Passwords never appear. No-op until host Initialize.
/// </summary>
internal static class HelperLog
{
    public const string Category = ProcessesCatalog.Category;

    public static class AppIds
    {
        public const string Processes = ProcessesCatalog.AppId;
    }

    public static class Subcategories
    {
        public const string Probe = "Probe";
        public const string Guard = "Guard";
        public const string Inventory = "Inventory";
        public const string Query = "Query";
        public const string Campaign = "Campaign";
        public const string Process = "Process";
        public const string Thread = "Thread";
        public const string Watch = "Watch";
        public const string Start = "Start";
        public const string Kill = "Kill";
        public const string System = "System";
        public const string Safety = "Safety";
    }

    private static readonly AsyncLocal<ScopeState?> Scope = new();
    public static string CurrentAppId => Scope.Value?.AppId ?? AppIds.Processes;
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
            appId: ProcessesCatalog.AppId);
    }

    private static VestigiumStatus Normalize(VestigiumStatus status)
        => status is VestigiumStatus.Success or VestigiumStatus.Warning or VestigiumStatus.Failed
            ? status
            : VestigiumStatus.Warning;

    private static int EventId(VestigiumLogLevel level, string subcategory, string message)
    {
        if (string.Equals(subcategory, AppIds.Processes, StringComparison.Ordinal)
            || string.Equals(subcategory, Subcategories.Probe, StringComparison.Ordinal))
        {
            if (message.Contains("probe complete", StringComparison.OrdinalIgnoreCase))
                return ProcessesEvents.ProbeComplete;
            if (level is VestigiumLogLevel.Information or VestigiumLogLevel.Debug)
                return ProcessesEvents.ProbeEnter;
        }

        return level switch
        {
            VestigiumLogLevel.Debug or VestigiumLogLevel.Verbose => ProcessesEvents.OperationEnter,
            VestigiumLogLevel.Warning => ProcessesEvents.OperationWarning,
            VestigiumLogLevel.Error or VestigiumLogLevel.Fatal => ProcessesEvents.OperationFailed,
            _ => ProcessesEvents.OperationComplete
        };
    }

    private static string CatalogMessage(int eventId) => eventId switch
    {
        ProcessesEvents.ProbeEnter => "enter Probe",
        ProcessesEvents.ProbeComplete => "probe complete",
        ProcessesEvents.OperationEnter => "enter operation",
        ProcessesEvents.OperationComplete => "operation complete",
        ProcessesEvents.OperationFailed => "operation failed",
        ProcessesEvents.OperationWarning => "operation warning",
        _ => "operation complete"
    };

    private sealed record ScopeState(
        string AppId, string Subcategory, string Method, string? CorrelationId, ScopeState? Parent);

    private sealed class PopScope(ScopeState? parent) : IDisposable
    {
        private int _done;

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _done, 1) == 1)
                return;
            Scope.Value = parent;
        }
    }
}
