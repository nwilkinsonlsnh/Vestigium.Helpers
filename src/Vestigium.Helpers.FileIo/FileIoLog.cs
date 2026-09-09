using Vestigium.Helpers;
using Vestigium.Logging;

namespace Vestigium.Helpers.FileIo;

/// <summary>
/// ALCOA+ door for FileIo. Category Helpers, APPID FileIo. Never file contents.
/// Live FileIoProgress is chatty; these JSONL lines stay sparse.
/// </summary>
internal static class FileIoLog
{
    private const string App = HelperLog.AppIds.FileIo;

    public static void Pending(string subcategory, string message)
        => HelperLog.Information(App, VestigiumStatus.Pending, subcategory, message);

    public static void Success(string subcategory, string message)
        => HelperLog.Information(App, VestigiumStatus.Success, subcategory, message);

    public static void Warning(string subcategory, string message)
        => HelperLog.Warning(App, VestigiumStatus.None, subcategory, message);

    public static void Failed(string subcategory, string message)
        => HelperLog.Error(App, VestigiumStatus.Failed, subcategory, message);

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
        if (t.Length >= 44 && RegexBase64.IsMatch(t))
            return true;
        return false;
    }

    static readonly System.Text.RegularExpressions.Regex RegexBase64 =
        new(@"^[A-Za-z0-9+/]{44,}={0,2}$", System.Text.RegularExpressions.RegexOptions.CultureInvariant);
}
