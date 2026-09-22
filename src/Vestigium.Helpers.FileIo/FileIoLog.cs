using System.Text.RegularExpressions;
using Vestigium.Logging;

namespace Vestigium.Helpers.FileIo;

/// <summary>
/// ALCOA+ door for FileIo. Never file contents. Live FileIoProgress stays chatty;
/// these JSONL lines stay sparse. Failed never attaches Exception.ToString().
/// </summary>
internal static partial class FileIoLog
{
    public static class Subcategories
    {
        public const string Job = "Job";
        public const string Recon = "Recon";
        public const string Copy = "Copy";
        public const string Move = "Move";
        public const string Delete = "Delete";
        public const string Mirror = "Mirror";
        public const string Analyze = "Analyze";
        public const string Compare = "Compare";
        public const string Probe = "Probe";
        public const string SecureDelete = "SecureDelete";
        public const string Prune = "Prune";
        public const string Index = "Index";
        public const string Stats = "Stats";
        public const string Progress = "Progress";
    }

    [GeneratedRegex(@"^[A-Za-z0-9+/]{44,}={0,2}$", RegexOptions.CultureInvariant)]
    private static partial Regex RegexBase64();

    public static string NewId() => Guid.NewGuid().ToString("N")[..12];

    public static IReadOnlyDictionary<string, string?> Props(params (string Key, string? Value)[] pairs)
    {
        var map = new Dictionary<string, string?>(pairs.Length, StringComparer.Ordinal);
        foreach (var (key, value) in pairs)
            map[key] = Safe(value);
        return map;
    }

    public static void Pending(
        string subcategory,
        string message,
        string? correlationId = null,
        IReadOnlyDictionary<string, string?>? properties = null)
    {
        if (string.Equals(subcategory, Subcategories.Probe, StringComparison.Ordinal))
        {
            Write(FileIoEvents.ProbeEnter, VestigiumLogLevel.Debug, VestigiumStatus.Pending,
                subcategory, "enter Probe", message, correlationId, properties);
            return;
        }

        Write(FileIoEvents.OperationEnter, VestigiumLogLevel.Debug, VestigiumStatus.Pending,
            subcategory, "enter operation", message, correlationId, properties);
    }

    public static void Success(
        string subcategory,
        string message,
        string? correlationId = null,
        IReadOnlyDictionary<string, string?>? properties = null)
    {
        if (string.Equals(subcategory, Subcategories.Probe, StringComparison.Ordinal))
        {
            Write(FileIoEvents.ProbeComplete, VestigiumLogLevel.Information, VestigiumStatus.Success,
                subcategory, "probe complete", message, correlationId, properties);
            return;
        }

        Write(FileIoEvents.OperationComplete, VestigiumLogLevel.Information, VestigiumStatus.Success,
            subcategory, "operation complete", message, correlationId, properties);
    }

    public static void Warning(
        string subcategory,
        string message,
        string? correlationId = null,
        IReadOnlyDictionary<string, string?>? properties = null)
        => Write(FileIoEvents.OperationWarning, VestigiumLogLevel.Warning, VestigiumStatus.Warning,
            subcategory, "operation warning", message, correlationId, properties);

    public static void Failed(
        string subcategory,
        string message,
        string? correlationId = null,
        IReadOnlyDictionary<string, string?>? properties = null)
        => Write(FileIoEvents.OperationFailed, VestigiumLogLevel.Error, VestigiumStatus.Failed,
            subcategory, "operation failed", message, correlationId, properties);

    public static void JobStart(string? correlationId, IReadOnlyDictionary<string, string?>? properties = null)
        => Write(FileIoEvents.JobStart, VestigiumLogLevel.Information, VestigiumStatus.Pending,
            Subcategories.Job, "job start", null, correlationId, properties);

    public static void JobComplete(string? correlationId, IReadOnlyDictionary<string, string?>? properties = null)
        => Write(FileIoEvents.JobComplete, VestigiumLogLevel.Information, VestigiumStatus.Success,
            Subcategories.Job, "job complete", null, correlationId, properties);

    public static void JobCancelled(string? correlationId, IReadOnlyDictionary<string, string?>? properties = null)
        => Write(FileIoEvents.JobCancelled, VestigiumLogLevel.Warning, VestigiumStatus.Warning,
            Subcategories.Job, "job cancelled", null, correlationId, properties);

    public static void ReconStart(string? correlationId, IReadOnlyDictionary<string, string?>? properties = null)
        => Write(FileIoEvents.ReconStart, VestigiumLogLevel.Debug, VestigiumStatus.Pending,
            Subcategories.Recon, "recon start", null, correlationId, properties);

    public static void ReconComplete(string? correlationId, IReadOnlyDictionary<string, string?>? properties = null)
        => Write(FileIoEvents.ReconComplete, VestigiumLogLevel.Information, VestigiumStatus.Success,
            Subcategories.Recon, "recon complete", null, correlationId, properties);

    public static void ConsumersReleased(string? correlationId, IReadOnlyDictionary<string, string?>? properties = null)
        => Write(FileIoEvents.ConsumersReleased, VestigiumLogLevel.Information, VestigiumStatus.Success,
            Subcategories.Job, "consumers released", null, correlationId, properties);

    public static void Decision(
        string subcategory,
        string kind,
        string? correlationId,
        IReadOnlyDictionary<string, string?>? properties = null)
    {
        var extra = new Dictionary<string, string?>(StringComparer.Ordinal) { ["kind"] = Safe(kind) };
        if (properties is not null)
        {
            foreach (var pair in properties)
            {
                if (!string.IsNullOrWhiteSpace(pair.Key))
                    extra[pair.Key] = pair.Value;
            }
        }
        Write(FileIoEvents.Decision, VestigiumLogLevel.Information, VestigiumStatus.Success,
            subcategory, "decision", null, correlationId, extra);
    }

    public static void NameCap(string subcategory, string? correlationId, IReadOnlyDictionary<string, string?>? properties = null)
        => Write(FileIoEvents.NameCap, VestigiumLogLevel.Error, VestigiumStatus.Failed,
            subcategory, "name cap", null, correlationId, properties);

    public static void ItemInUse(string subcategory, string? correlationId, IReadOnlyDictionary<string, string?>? properties = null)
        => Write(FileIoEvents.ItemInUse, VestigiumLogLevel.Error, VestigiumStatus.Failed,
            subcategory, "in use", null, correlationId, properties);

    public static void ItemUnauthorized(string subcategory, string? correlationId, IReadOnlyDictionary<string, string?>? properties = null)
        => Write(FileIoEvents.ItemUnauthorized, VestigiumLogLevel.Error, VestigiumStatus.Failed,
            subcategory, "unauthorized", null, correlationId, properties);

    public static void IndexBuilt(string? correlationId, IReadOnlyDictionary<string, string?>? properties = null)
        => Write(FileIoEvents.IndexBuilt, VestigiumLogLevel.Information, VestigiumStatus.Success,
            Subcategories.Index, "index built", null, correlationId, properties);

    public static void IndexHit(string? correlationId, IReadOnlyDictionary<string, string?>? properties = null)
        => Write(FileIoEvents.IndexHit, VestigiumLogLevel.Information, VestigiumStatus.Success,
            Subcategories.Index, "skip duplicate", null, correlationId, properties);

    public static void ProgressSnapshot(string? correlationId, IReadOnlyDictionary<string, string?>? properties = null)
        => Write(FileIoEvents.ProgressSnapshot, VestigiumLogLevel.Information, VestigiumStatus.Success,
            Subcategories.Progress, "progress snapshot", null, correlationId, properties);

    public static void StatsFinalize(string? correlationId, IReadOnlyDictionary<string, string?>? properties = null)
        => Write(FileIoEvents.StatsFinalize, VestigiumLogLevel.Information, VestigiumStatus.Success,
            Subcategories.Stats, "stats", null, correlationId, properties);

    public static void JobPaused(string? correlationId, IReadOnlyDictionary<string, string?>? properties = null)
        => Write(FileIoEvents.JobPaused, VestigiumLogLevel.Warning, VestigiumStatus.Warning,
            Subcategories.Job, "pause", null, correlationId, properties);

    public static void JobResumed(string? correlationId, IReadOnlyDictionary<string, string?>? properties = null)
        => Write(FileIoEvents.JobResumed, VestigiumLogLevel.Information, VestigiumStatus.Success,
            Subcategories.Job, "resume", null, correlationId, properties);

    public static IDisposable Begin(string subcategory, string method, string? correlationId = null)
    {
        Pending(subcategory, method, correlationId);
        return NullScope.Instance;
    }

    public static string RequireNotBlank(string? value, string paramName)
    {
        if (!string.IsNullOrWhiteSpace(value))
            return value.Trim();
        Write(FileIoEvents.PathRejected, VestigiumLogLevel.Error, VestigiumStatus.Failed,
            Subcategories.Job, "rejected path", paramName);
        throw new ArgumentException("Value is required.", paramName);
    }

    public static string FileExists(string? path, string paramName)
    {
        var file = RequireNotBlank(path, paramName);
        if (System.IO.File.Exists(file))
            return file;
        Write(FileIoEvents.PathRejected, VestigiumLogLevel.Error, VestigiumStatus.Failed,
            Subcategories.Job, "rejected path", file);
        throw new FileNotFoundException("Source file was not found.", file);
    }

    public static bool LooksLikeSecret(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return false;
        if (value.Contains("BEGIN ", StringComparison.OrdinalIgnoreCase)
            || value.Contains("PRIVATE KEY", StringComparison.OrdinalIgnoreCase)
            || value.Contains("-----", StringComparison.Ordinal))
            return true;
        var t = value.Trim();
        if (t.Length >= 32 && t.All(Uri.IsHexDigit))
            return true;
        if (t.Length >= 44 && RegexBase64().IsMatch(t))
            return true;
        return false;
    }

    public static string Safe(string? message)
    {
        if (string.IsNullOrEmpty(message))
            return message ?? string.Empty;
        return LooksLikeSecret(message) ? "[redacted]" : message;
    }

    /// <summary>
    /// Door to Vestigium.Logging. Probe / Analyze / Compare leave <paramref name="correlationId"/> null unless a job passed one.
    /// Never attaches <see cref="Exception"/>.
    /// </summary>
    internal static void Write(
        int eventId,
        VestigiumLogLevel level,
        VestigiumStatus status,
        string subcategory,
        string message,
        string? detail = null,
        string? correlationId = null,
        IReadOnlyDictionary<string, string?>? properties = null)
    {
        if (!VestigiumLogger.IsInitialized)
            return;

        VestigiumLog.Write(
            eventId,
            level,
            status,
            FileIoCatalog.Category,
            subcategory,
            message,
            exception: null,
            correlationId: string.IsNullOrWhiteSpace(correlationId) ? null : correlationId.Trim(),
            properties: Merge(subcategory, detail, properties),
            appId: FileIoCatalog.AppId);
    }

    private static IReadOnlyDictionary<string, string?> Merge(
        string subcategory,
        string? detail,
        IReadOnlyDictionary<string, string?>? extra)
    {
        var map = new Dictionary<string, string?>(StringComparer.Ordinal)
        {
            ["subcategory"] = Safe(subcategory)
        };
        if (!string.IsNullOrEmpty(detail))
            map["detail"] = Safe(detail);
        if (extra is null)
            return map;
        foreach (var pair in extra)
        {
            if (string.IsNullOrWhiteSpace(pair.Key))
                continue;
            map[pair.Key] = Safe(pair.Value);
        }
        return map;
    }

    private sealed class NullScope : IDisposable
    {
        public static readonly NullScope Instance = new();
        public void Dispose() { }
    }
}
