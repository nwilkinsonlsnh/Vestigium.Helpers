using Vestigium.Logging;

namespace Vestigium.Helpers.Network;

/// <summary>
/// Network-only door into Vestigium.Logging. Same call surface engines already use.
/// Never packet payloads, WLAN keys, or Exception.ToString(). No-op until host Initialize.
/// </summary>
internal static class HelperLog
{
    public const string Category = NetworkCatalog.Category;

    public static class AppIds
    {
        public const string Network = NetworkCatalog.AppId;
    }

    public static class Subcategories
    {
        public const string Probe = "Probe";
        public const string Guard = "Guard";
        public const string Inventory = "Inventory";
        public const string Adapter = "Adapter";
        public const string Connection = "Connection";
        public const string Neighbor = "Neighbor";
        public const string Netbios = "Netbios";
        public const string Route = "Route";
        public const string Icmp = "Icmp";
        public const string Dns = "Dns";
        public const string Campaign = "Campaign";
        public const string Share = "Share";
        public const string Address = "Address";
        public const string Bandwidth = "Bandwidth";
        public const string Subnet = "Subnet";
        public const string Stats = "Stats";
        public const string Progress = "Progress";
    }

    private static readonly AsyncLocal<ScopeState?> Scope = new();

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

    public static void Reject(
        string appId,
        string subcategory,
        string method,
        string? reason,
        string? correlationId = null,
        Exception? exception = null)
        => Error(appId, VestigiumStatus.Failed, subcategory, Line("reject", method, reason, correlationId), exception);

    public static void Debug(string appId, VestigiumStatus status, string subcategory, string message, Exception? exception = null)
        => Write(appId, VestigiumLogLevel.Debug, status, subcategory, message, exception);

    public static void Information(string appId, VestigiumStatus status, string subcategory, string? message, Exception? exception = null)
        => Write(appId, VestigiumLogLevel.Information, status, subcategory, message, exception);

    public static void Warning(string appId, VestigiumStatus status, string subcategory, string? message, Exception? exception = null)
        => Write(appId, VestigiumLogLevel.Warning, status, subcategory, message, exception);

    public static void Error(string appId, VestigiumStatus status, string subcategory, string? message, Exception? exception = null)
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
        string? message,
        Exception? exception)
    {
        message ??= string.Empty;
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
            appId: NetworkCatalog.AppId);
    }

    private static VestigiumStatus Normalize(VestigiumStatus status)
        => status is VestigiumStatus.Success or VestigiumStatus.Warning or VestigiumStatus.Failed
            ? status
            : VestigiumStatus.Warning;

    private static int EventId(VestigiumLogLevel level, string subcategory, string message)
    {
        if (!string.Equals(subcategory, Subcategories.Probe, StringComparison.Ordinal))
            return level switch
            {
                VestigiumLogLevel.Debug or VestigiumLogLevel.Verbose => NetworkEvents.OperationEnter,
                VestigiumLogLevel.Warning => NetworkEvents.OperationWarning,
                VestigiumLogLevel.Error or VestigiumLogLevel.Fatal => NetworkEvents.OperationFailed,
                _ => NetworkEvents.OperationComplete
            };
        return level switch
        {
            VestigiumLogLevel.Debug => NetworkEvents.ProbeEnter,
            VestigiumLogLevel.Information => NetworkEvents.ProbeComplete,
            _ => level switch
            {
                VestigiumLogLevel.Debug or VestigiumLogLevel.Verbose => NetworkEvents.OperationEnter,
                VestigiumLogLevel.Warning => NetworkEvents.OperationWarning,
                VestigiumLogLevel.Error or VestigiumLogLevel.Fatal => NetworkEvents.OperationFailed,
                _ => NetworkEvents.OperationComplete
            }
        };
    }

    private static string CatalogMessage(int eventId) => eventId switch
    {
        NetworkEvents.ProbeEnter => "enter Probe",
        NetworkEvents.ProbeComplete => "probe complete",
        NetworkEvents.OperationEnter => "enter operation",
        NetworkEvents.OperationComplete => "operation complete",
        NetworkEvents.OperationFailed => "operation failed",
        NetworkEvents.OperationWarning => "operation warning",
        NetworkEvents.RouteDenied => "route write denied",
        NetworkEvents.IcmpForbidden => "ICMP not permitted",
        NetworkEvents.CampaignWindowMissed => "campaign window missed",
        NetworkEvents.CampaignPathEscape => "campaign path escape",
        NetworkEvents.DnsPeerMismatch => "DNS peer mismatch",
        NetworkEvents.OuiLookupRejected => "OUI lookup rejected",
        _ => "operation complete"
    };

    public static void WriteEvent(
        int eventId,
        VestigiumLogLevel level,
        VestigiumStatus status,
        string subcategory,
        string? message)
    {
        if (!VestigiumLogger.IsInitialized)
            return;

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
                ["appId"] = AppIds.Network,
                ["detail"] = message ?? string.Empty
            },
            appId: NetworkCatalog.AppId);
    }

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
