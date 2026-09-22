using System.Text.RegularExpressions;

namespace Vestigium.Helpers.FileIo;

/// <summary>
/// One wildcard rule for Job recon and Analyze. <c>*</c> and <c>?</c>, case-insensitive, whole name.
/// </summary>
public static class FileIoMask
{
    public static bool Matches(string name, IReadOnlyList<string>? masks)
    {
        if (string.IsNullOrEmpty(name) || masks is null || masks.Count == 0)
            return false;
        foreach (var mask in masks)
        {
            if (string.IsNullOrWhiteSpace(mask))
                continue;
            var body = Regex.Escape(mask).Replace("\\*", ".*").Replace("\\?", ".");
            if (Regex.IsMatch(name, "^" + body + "$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
                return true;
        }
        return false;
    }
}
