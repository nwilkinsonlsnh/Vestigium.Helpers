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
        return string.IsNullOrWhiteSpace(correlationId)
               || message.Contains("session=", StringComparison.Ordinal)
               || message.Contains("id=", StringComparison.Ordinal)
            ? message
            : message + " id=" + correlationId;
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
            if (level is VestigiumLogLevel.Debug && message.StartsWith("enter", StringComparison.Ordinal))
                return JsonEvents.ProbeEnter;
            if (level is VestigiumLogLevel.Information)
                return JsonEvents.ProbeComplete;
        }

        return subcategory switch
        {
            Subcategories.Session => Map(level, JsonEvents.SessionEnter, JsonEvents.SessionComplete, JsonEvents.SessionFailed),
            Subcategories.Document => Map(level, JsonEvents.DocumentEnter, JsonEvents.DocumentComplete, JsonEvents.DocumentFailed),
            Subcategories.Query => Map(level, JsonEvents.QueryEnter, JsonEvents.QueryComplete, JsonEvents.QueryFailed),
            Subcategories.Snapshot => Map(level, JsonEvents.SnapshotEnter, JsonEvents.SnapshotComplete, JsonEvents.SnapshotFailed),
            Subcategories.Diff => Map(level, JsonEvents.DiffEnter, JsonEvents.DiffComplete, JsonEvents.DiffFailed),
            Subcategories.Commit => Map(level, JsonEvents.CommitEnter, JsonEvents.CommitComplete, JsonEvents.CommitFailed),
            Subcategories.Save => level switch
            {
                VestigiumLogLevel.Debug or VestigiumLogLevel.Verbose => JsonEvents.SaveEnter,
                VestigiumLogLevel.Warning => JsonEvents.SaveWarning,
                VestigiumLogLevel.Error or VestigiumLogLevel.Fatal => JsonEvents.SaveFailed,
                _ => JsonEvents.SaveComplete
            },
            Subcategories.Jsonl => Map(level, JsonEvents.JsonlEnter, JsonEvents.JsonlComplete, JsonEvents.JsonlFailed),
            Subcategories.Guard => level is VestigiumLogLevel.Warning
                ? JsonEvents.OperationWarning
                : JsonEvents.GuardFailed,
            Subcategories.Probe => Fallback(level),
            _ => Fallback(level)
        };
    }

    private static int Map(VestigiumLogLevel level, int enter, int complete, int failed)
        => level switch
        {
            VestigiumLogLevel.Debug or VestigiumLogLevel.Verbose => enter,
            VestigiumLogLevel.Error or VestigiumLogLevel.Fatal => failed,
            VestigiumLogLevel.Warning => JsonEvents.OperationWarning,
            _ => complete
        };

    private static int Fallback(VestigiumLogLevel level)
        => level switch
        {
            VestigiumLogLevel.Warning => JsonEvents.OperationWarning,
            VestigiumLogLevel.Error or VestigiumLogLevel.Fatal => JsonEvents.GuardFailed,
            _ => JsonEvents.SessionComplete
        };

    private static string CatalogMessage(int eventId) => eventId switch
    {
        JsonEvents.ProbeEnter => "enter Probe",
        JsonEvents.ProbeComplete => "probe complete",
        JsonEvents.SessionEnter => "enter session",
        JsonEvents.SessionComplete => "session complete",
        JsonEvents.SessionFailed => "session failed",
        JsonEvents.DocumentEnter => "enter document",
        JsonEvents.DocumentComplete => "document complete",
        JsonEvents.DocumentFailed => "document failed",
        JsonEvents.QueryEnter => "enter query",
        JsonEvents.QueryComplete => "query complete",
        JsonEvents.QueryFailed => "query failed",
        JsonEvents.SnapshotEnter => "enter snapshot",
        JsonEvents.SnapshotComplete => "snapshot complete",
        JsonEvents.SnapshotFailed => "snapshot failed",
        JsonEvents.DiffEnter => "enter diff",
        JsonEvents.DiffComplete => "diff complete",
        JsonEvents.DiffFailed => "diff failed",
        JsonEvents.CommitEnter => "enter commit",
        JsonEvents.CommitComplete => "commit complete",
        JsonEvents.CommitFailed => "commit failed",
        JsonEvents.SaveEnter => "enter save",
        JsonEvents.SaveComplete => "save complete",
        JsonEvents.SaveWarning => "save warning",
        JsonEvents.SaveFailed => "save failed",
        JsonEvents.JsonlEnter => "enter jsonl",
        JsonEvents.JsonlComplete => "jsonl complete",
        JsonEvents.JsonlFailed => "jsonl failed",
        JsonEvents.GuardFailed => "guard failed",
        JsonEvents.OperationWarning => "operation warning",
        _ => "session complete"
    };

    private sealed record ScopeState(
        string AppId,
        string Subcategory,
        string Method,
        string? CorrelationId,
        ScopeState? Parent);

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
