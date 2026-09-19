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

    public static void Pending(string subcategory, string message)
    {
        if (string.Equals(subcategory, Subcategories.Probe, StringComparison.Ordinal))
        {
            Write(FileIoEvents.ProbeEnter, VestigiumLogLevel.Debug, VestigiumStatus.Pending, subcategory, "enter Probe", message);
            return;
        }

        Write(FileIoEvents.OperationEnter, VestigiumLogLevel.Debug, VestigiumStatus.Pending, subcategory, "enter operation", message);
    }

    public static void Success(string subcategory, string message)
    {
        if (string.Equals(subcategory, Subcategories.Probe, StringComparison.Ordinal))
        {
            Write(FileIoEvents.ProbeComplete, VestigiumLogLevel.Information, VestigiumStatus.Success, subcategory, "probe complete", message);
            return;
        }

        Write(FileIoEvents.OperationComplete, VestigiumLogLevel.Information, VestigiumStatus.Success, subcategory, "operation complete", message);
    }

    public static void Warning(string subcategory, string message)
        => Write(FileIoEvents.OperationWarning, VestigiumLogLevel.Warning, VestigiumStatus.Warning, subcategory, "operation warning", message);

    public static void Failed(string subcategory, string message)
        => Write(FileIoEvents.OperationFailed, VestigiumLogLevel.Error, VestigiumStatus.Failed, subcategory, "operation failed", message);

    public static IDisposable Begin(string subcategory, string method)
    {
        Pending(subcategory, method);
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

    private static void Write(
        int eventId,
        VestigiumLogLevel level,
        VestigiumStatus status,
        string subcategory,
        string message,
        string detail)
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
            correlationId: null,
            properties: Props(("detail", detail), ("subcategory", subcategory)),
            appId: FileIoCatalog.AppId);
    }

    private sealed class NullScope : IDisposable
    {
        public static readonly NullScope Instance = new();
        public void Dispose() { }
    }
}
