using Vestigium.Logging;

namespace Vestigium.Helpers.Json;

/// <summary>
/// Json-only door into Vestigium.Logging. Same call surface the existing files already use.
/// Writes are no-ops until the host initializes. Failed never attaches Exception.ToString().
/// </summary>
internal static class HelperLog
{
    public const string Category = JsonCatalog.Category;

    public static class AppIds
    {
        public const string Json = JsonCatalog.AppId;
    }

    public static class Subcategories
    {
        public const string Probe = "Probe";
        public const string Guard = "Guard";
        public const string Session = "Session";
        public const string Document = "Document";
        public const string Query = "Query";
        public const string Snapshot = "Snapshot";
        public const string Diff = "Diff";
        public const string Commit = "Commit";
        public const string Save = "Save";
        public const string Jsonl = "Jsonl";
    }

    private static readonly AsyncLocal<ScopeState?> Scope = new();
    private static readonly AsyncLocal<bool> ContractFailed = new();

    public static string CurrentAppId => Scope.Value?.AppId ?? AppIds.Json;
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
        if (parent is null)
            ContractFailed.Value = false;
        var id = correlationId ?? parent?.CorrelationId;
        Scope.Value = new ScopeState(appId, subcategory, method, id, parent);
        Enter(appId, subcategory, method, detail, id);
        return new PopScope(parent);
    }

    public static void Enter(
        string appId,
        string subcategory,
        string method,
        string? detail = null,
        string? correlationId = null)
        => Debug(appId, VestigiumStatus.Pending, subcategory, Line("enter", method, detail, correlationId));

    public static void Exit(
        string appId,
        string subcategory,
        string method,
        string? detail = null,
        string? correlationId = null)
        => Debug(appId, VestigiumStatus.Success, subcategory, Line("exit", method, detail, correlationId));

    public static void Reject(string reason, Exception? exception = null)
        => Reject(CurrentAppId, CurrentSubcategory, CurrentMethod, reason, CorrelationId, exception);

    public static void Reject(
        string appId,
        string subcategory,
        string method,
        string reason,
        string? correlationId = null,
        Exception? exception = null)
    {
        ContractFailed.Value = true;
        Error(appId, VestigiumStatus.Failed, subcategory, Line("reject", method, reason, correlationId), exception);
    }

    public static void Trap(Exception ex)
    {
        if (ContractFailed.Value)
            return;
        ContractFailed.Value = true;
        Error(
            CurrentAppId,
            VestigiumStatus.Failed,
            CurrentSubcategory,
            Line("failed", CurrentMethod, $"{ex.GetType().Name}: {ex.Message}", CorrelationId),
            ex);
    }

    public static string Line(string verb, string method, string? detail, string? correlationId)
    {
        var message = string.IsNullOrWhiteSpace(detail)
            ? $"{verb} {method}"
            : $"{verb} {method} {detail}";
        if (string.IsNullOrWhiteSpace(correlationId))
            return message;
        if (message.Contains("session=", StringComparison.Ordinal)
            || message.Contains("id=", StringComparison.Ordinal))
            return message;
        return message + " id=" + correlationId;
    }

    public static void Debug(string appId, VestigiumStatus status, string subcategory, string message, Exception? exception = null)
        => Write(appId, VestigiumLogLevel.Debug, status, subcategory, message, exception);

    public static void Information(string appId, VestigiumStatus status, string subcategory, string message, Exception? exception = null)
        => Write(appId, VestigiumLogLevel.Information, status, subcategory, message, exception);

    public static void Warning(string appId, VestigiumStatus status, string subcategory, string message, Exception? exception = null)
        => Write(appId, VestigiumLogLevel.Warning, status, subcategory, message, exception);

    public static void Error(string appId, VestigiumStatus status, string subcategory, string message, Exception? exception = null)
        => Write(appId, VestigiumLogLevel.Error, status, subcategory, message, exception);

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
            status,
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
            appId: JsonCatalog.AppId);
    }

    private static int EventId(VestigiumLogLevel level, string subcategory, string message)
    {
        if (string.Equals(subcategory, Subcategories.Probe, StringComparison.Ordinal))
        {
            if (level == VestigiumLogLevel.Debug && message.StartsWith("enter", StringComparison.Ordinal))
                return JsonEvents.ProbeEnter;
            if (level == VestigiumLogLevel.Information)
                return JsonEvents.ProbeComplete;
        }

        return level switch
        {
            VestigiumLogLevel.Debug or VestigiumLogLevel.Verbose => JsonEvents.OperationEnter,
            VestigiumLogLevel.Warning => JsonEvents.OperationWarning,
            VestigiumLogLevel.Error or VestigiumLogLevel.Fatal => JsonEvents.OperationFailed,
            _ => JsonEvents.OperationComplete
        };
    }

    private static string CatalogMessage(int eventId) => eventId switch
    {
        JsonEvents.ProbeEnter => "enter Probe",
        JsonEvents.ProbeComplete => "probe complete",
        JsonEvents.OperationEnter => "enter operation",
        JsonEvents.OperationComplete => "operation complete",
        JsonEvents.OperationFailed => "operation failed",
        JsonEvents.OperationWarning => "operation warning",
        _ => "operation complete"
    };

    private sealed record ScopeState(
        string AppId,
        string Subcategory,
        string Method,
        string? CorrelationId,
        ScopeState? Parent);

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
