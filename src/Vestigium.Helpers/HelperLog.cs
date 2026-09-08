using Vestigium.Logging;

namespace Vestigium.Helpers;

/// <summary>
/// Safe façade over Vestigium.Logging for helper libraries.
/// Libraries never call Initialize. WPF galleries and application hosts do.
/// Writes are no-ops until the host has initialized the logger.
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
        t.Register(Category, "Probe", "Identity", "Guard");
        return t;
    }
}
