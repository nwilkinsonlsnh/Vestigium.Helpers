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
        return (from mask in masks where !string.IsNullOrWhiteSpace(mask) select Regex.Escape(mask).Replace("\\*", ".*").Replace("\\?", ".")).Any(body => Regex.IsMatch(name, "^" + body + "$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant));
    }
}
