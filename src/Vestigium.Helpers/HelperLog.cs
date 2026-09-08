using Vestigium.Logging;

namespace Vestigium.Helpers;

/// <summary>
/// Safe façade over Vestigium.Logging for helper libraries.
/// Libraries never call Initialize. WPF galleries and application hosts do.
/// Writes are no-ops until the host has initialized the logger.
/// Enter/argument lines are always compiled in; the host floor
/// (<see cref="VestigiumLoggerOptions.MinimumDiskLevel"/>) decides whether
/// they land on disk. The in-memory ring still sees Debug when the host is on.
/// </summary>
public static class HelperLog
{
    public const string Category = "Helpers";

    public static class AppIds
    {
        public const string Core = "Helpers";
        public const string ClosedXml = "ClosedXml";
        public const string Encryption = "Encryption";
        public const string WinReg = "WinReg";
        public const string Json = "Json";
        public const string Xml = "Xml";
        public const string FileIo = "FileIo";
        public const string Processes = "Processes";
        public const string Services = "Services";
        public const string Analytics = "Analytics";
        public const string Network = "Network";
        public const string Csv = "Csv";
    }

    public static class Subcategories
    {
        public const string Probe = "Probe";
        public const string Identity = "Identity";
        public const string Guard = "Guard";
        public const string Session = "Session";
        public const string Sheet = "Sheet";
        public const string Series = "Series";
        public const string Confidence = "Confidence";
        public const string Chart = "Chart";
    }

    public static IReadOnlyList<string> AllAppIds { get; } =
    [
        AppIds.Core,
        AppIds.ClosedXml,
        AppIds.Encryption,
        AppIds.WinReg,
        AppIds.Json,
        AppIds.Xml,
        AppIds.FileIo,
        AppIds.Processes,
        AppIds.Services,
        AppIds.Analytics,
        AppIds.Network,
        AppIds.Csv
    ];

    public static VestigiumTaxonomy Taxonomy { get; } = CreateTaxonomy();

    private static readonly AsyncLocal<ScopeState?> Scope = new();

    public static string CurrentAppId => Scope.Value?.AppId ?? AppIds.Core;

    public static string CurrentMethod => Scope.Value?.Method ?? Subcategories.Guard;

    public static string CurrentSubcategory => Scope.Value?.Subcategory ?? Subcategories.Guard;

    public static string? CorrelationId => Scope.Value?.CorrelationId;

    public static void ConfigureHost(VestigiumLoggerOptions cfg, string appId)
    {
        ArgumentNullException.ThrowIfNull(cfg);
        cfg.AppId = HelperGuard.NotBlank(appId, nameof(appId));
        cfg.RegisterTaxonomy(VestigiumTaxonomy.Defaults);
        cfg.RegisterTaxonomy(Taxonomy);
        cfg.LogDirectory ??= Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
            "Vestigium", "Logs", cfg.AppId);
    }

    public static void InitializeHost(string appId, Action<VestigiumLoggerOptions>? extra = null, object? wpfApplication = null)
    {
        VestigiumLogger.Initialize(cfg =>
        {
            ConfigureHost(cfg, appId);
            extra?.Invoke(cfg);
        });
        VestigiumLogger.BindLifetime(wpfApplication);
    }

    public static bool IsInitialized => VestigiumLogger.IsInitialized;

    public static string LogDirectory =>
        VestigiumLogger.IsInitialized ? VestigiumLogger.Options.ResolveLogDirectory() : string.Empty;

    public static IReadOnlyList<string> RecentJsonLines =>
        VestigiumLogger.IsInitialized ? VestigiumLogger.RecentJsonLines : [];

    public static void Flush()
    {
        if (VestigiumLogger.IsInitialized)
            VestigiumLogger.Flush();
    }

    public static void Shutdown()
    {
        if (VestigiumLogger.IsInitialized)
            VestigiumLogger.Shutdown();
    }

    public static string NewId() => Guid.NewGuid().ToString("N")[..8];

    /// <summary>
    /// Push app / method / correlation for nested guards, and write a Debug enter line.
    /// Dispose restores the previous scope. Does not write exit — call
    /// <see cref="Exit"/> after a successful return, or let a reject
    /// record the failure.
    /// </summary>
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
        => Error(appId, VestigiumStatus.Failed, subcategory, Line("reject", method, reason, correlationId), exception);

    public static string Line(string verb, string method, string? detail, string? correlationId)
    {
        var message = string.IsNullOrWhiteSpace(detail)
            ? $"{verb} {method}"
            : $"{verb} {method} {detail}";
        if (string.IsNullOrWhiteSpace(correlationId))
            return message;
        if (message.Contains("session=", StringComparison.Ordinal)
            || message.Contains("series=", StringComparison.Ordinal)
            || message.Contains("id=", StringComparison.Ordinal))
            return message;
        return message + " id=" + correlationId;
    }

    public static void Write(
        string helperAppId,
        VestigiumLogLevel level,
        VestigiumStatus status,
        string subcategory,
        string message,
        Exception? exception = null)
    {
        if (!VestigiumLogger.IsInitialized)
            return;

        VestigiumLog.Write(
            level,
            status,
            Category,
            subcategory,
            message,
            exception,
            helperAppId);
    }

    public static void Verbose(string appId, VestigiumStatus status, string subcategory, string message, Exception? exception = null)
        => Write(appId, VestigiumLogLevel.Verbose, status, subcategory, message, exception);

    public static void Debug(string appId, VestigiumStatus status, string subcategory, string message, Exception? exception = null)
        => Write(appId, VestigiumLogLevel.Debug, status, subcategory, message, exception);

    public static void Information(string appId, VestigiumStatus status, string subcategory, string message, Exception? exception = null)
        => Write(appId, VestigiumLogLevel.Information, status, subcategory, message, exception);

    public static void Warning(string appId, VestigiumStatus status, string subcategory, string message, Exception? exception = null)
        => Write(appId, VestigiumLogLevel.Warning, status, subcategory, message, exception);

    public static void Error(string appId, VestigiumStatus status, string subcategory, string message, Exception? exception = null)
        => Write(appId, VestigiumLogLevel.Error, status, subcategory, message, exception);

    public static void Fatal(string appId, VestigiumStatus status, string subcategory, string message, Exception? exception = null)
        => Write(appId, VestigiumLogLevel.Fatal, status, subcategory, message, exception);

    private static VestigiumTaxonomy CreateTaxonomy()
    {
        var t = new VestigiumTaxonomy();
        t.Register(Category, AllAppIds.ToArray());
        t.Register(
            Category,
            Subcategories.Probe,
            Subcategories.Identity,
            Subcategories.Guard,
            Subcategories.Session,
            Subcategories.Sheet,
            Subcategories.Series,
            Subcategories.Confidence,
            Subcategories.Chart);
        return t;
    }

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
