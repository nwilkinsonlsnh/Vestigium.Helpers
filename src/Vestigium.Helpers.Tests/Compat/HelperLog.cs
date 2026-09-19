using Vestigium.Helpers.Analytics;
using Vestigium.Helpers.Charts;
using Vestigium.Helpers.ClosedXml;
using Vestigium.Helpers.Csv;
using Vestigium.Helpers.Encryption;
using Vestigium.Helpers.FileIo;
using Vestigium.Helpers.Hashing;
using Vestigium.Helpers.Json;
using Vestigium.Helpers.Kql;
using Vestigium.Helpers.Network;
using Vestigium.Helpers.Processes;
using Vestigium.Helpers.Services;
using Vestigium.Helpers.WinReg;
using Vestigium.Helpers.Xml;
using Vestigium.Logging;

namespace Vestigium.Helpers;

/// <summary>
/// Test-only stand-in for the deleted core HelperLog. Hosts initialize Vestigium.Logging.
/// </summary>
public static class HelperLog
{
    public const string Category = "Helpers";

    public static class AppIds
    {
        public const string Core = "Core";
        public const string Analytics = AnalyticsCatalog.AppId;
        public const string Charts = ChartsCatalog.AppId;
        public const string ClosedXml = ClosedXmlCatalog.AppId;
        public const string Csv = CsvCatalog.AppId;
        public const string Encryption = EncryptionCatalog.AppId;
        public const string FileIo = FileIoCatalog.AppId;
        public const string Hashing = HashingCatalog.AppId;
        public const string Json = JsonCatalog.AppId;
        public const string Kql = KqlLoggingCatalog.AppId;
        public const string Network = NetworkCatalog.AppId;
        public const string Processes = ProcessesCatalog.AppId;
        public const string Services = ServicesCatalog.AppId;
        public const string WinReg = WinRegCatalog.AppId;
        public const string Xml = XmlCatalog.AppId;
    }

    public static class Subcategories
    {
        public const string Probe = "Probe";
        public const string Guard = "Guard";
        public const string Identity = "Identity";
        public const string Job = "Job";
        public const string Recon = "Recon";
        public const string Copy = "Copy";
        public const string Move = "Move";
        public const string Delete = "Delete";
        public const string Mirror = "Mirror";
        public const string Index = "Index";
        public const string Progress = "Progress";
        public const string Compare = "Compare";
        public const string Prune = "Prune";
        public const string SecureDelete = "SecureDelete";
        public const string Session = "Session";
        public const string Document = "Document";
        public const string Query = "Query";
        public const string Snapshot = "Snapshot";
        public const string Diff = "Diff";
        public const string Commit = "Commit";
        public const string Save = "Save";
        public const string Jsonl = "Jsonl";
        public const string Stats = "Stats";
    }

    public static readonly TaxonomyGate Taxonomy = new();

    public static bool IsInitialized => VestigiumLogger.IsInitialized;
    public static IReadOnlyList<string> RecentJsonLines => VestigiumLogger.RecentJsonLines;
    public static string? LogDirectory { get; private set; }

    public static void Shutdown() => VestigiumLogger.Shutdown();

    public static void InitializeHost(string appId, Action<VestigiumLoggerOptions>? configure = null)
    {
        var dir = Path.Combine(Path.GetTempPath(), "VestigiumHelpersTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        VestigiumLogger.Initialize(cfg =>
        {
            cfg.AppId = appId;
            cfg.LogDirectory = dir;
            cfg.MinimumDiskLevel = VestigiumLogLevel.Debug;
            cfg.OperationsLogEnabled = false;
            configure?.Invoke(cfg);
            LogDirectory = cfg.LogDirectory;
            RegisterAll(cfg);
        });
    }

    public static void Information(string appId, VestigiumStatus status, string subcategory, string message)
    {
        if (!VestigiumLogger.IsInitialized)
            return;
        VestigiumLog.Information(10000, status, Category, subcategory, message, appId: appId);
    }

    private static void RegisterAll(VestigiumLoggerOptions cfg)
    {
        AnalyticsCatalog.Register(cfg);
        ChartsCatalog.Register(cfg);
        ClosedXmlCatalog.Register(cfg);
        CsvCatalog.Register(cfg);
        EncryptionCatalog.Register(cfg);
        FileIoCatalog.Register(cfg);
        HashingCatalog.Register(cfg);
        JsonCatalog.Register(cfg);
        KqlLoggingCatalog.Register(cfg);
        NetworkCatalog.Register(cfg);
        ProcessesCatalog.Register(cfg);
        ServicesCatalog.Register(cfg);
        WinRegCatalog.Register(cfg);
        XmlCatalog.Register(cfg);
    }

    public sealed class TaxonomyGate
    {
        public bool IsSubcategoryRegistered(string category, string subcategory) =>
            !string.IsNullOrWhiteSpace(category) && !string.IsNullOrWhiteSpace(subcategory);
    }
}

public static class HelperDemoHost
{
    public static int Run(string appId, string identity, Action body)
    {
        try
        {
            body();
            return 0;
        }
        catch
        {
            return 1;
        }
    }
}
