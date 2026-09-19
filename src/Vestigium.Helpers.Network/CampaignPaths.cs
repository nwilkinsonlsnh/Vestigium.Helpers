using Vestigium.Helpers;

namespace Vestigium.Helpers.Network;

internal static class CampaignPaths
{
    public static string Root()
    {
        if (!string.IsNullOrWhiteSpace(NetworkTestHooks.CampaignRoot))
            return Path.GetFullPath(NetworkTestHooks.CampaignRoot);

        return OperatingSystem.IsWindows()
            ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Vestigium", "Network", "Campaigns")
            : "/var/lib/vestigium/network/campaigns";
    }

    public static string Confine(string path, string paramName)
    {
        var raw = HelperGuard.NotBlank(path, paramName);
        string full;
        try
        {
            full = Path.GetFullPath(raw);
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
        {
            HelperLog.Reject(HelperLog.AppIds.Network, HelperLog.Subcategories.Campaign, nameof(Confine), "bad path");
            throw new ArgumentException("Campaign path is not valid.", paramName, ex);
        }

        if (!IsUnder(full, Root()))
        {
            HelperLog.Reject(HelperLog.AppIds.Network, HelperLog.Subcategories.Campaign, nameof(Confine), "path escape");
            throw new ArgumentException("Campaign path must stay under the campaign root.", paramName);
        }

        return full;
    }

    public static bool IsUnder(string fullPath, string root)
    {
        var full = Path.GetFullPath(fullPath);
        var baseRoot = Path.GetFullPath(root);
        var comparison = OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
        if (full.Equals(baseRoot, comparison))
            return true;

        var prefix = baseRoot.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                     + Path.DirectorySeparatorChar;
        return full.StartsWith(prefix, comparison);
    }

    public static void EnsureDirectoryUnderRoot(string path)
    {
        var full = Path.GetFullPath(path);
        var dir = Path.GetDirectoryName(full);
        if (string.IsNullOrWhiteSpace(dir))
            return;

        if (!IsUnder(dir, Root()))
        {
            HelperLog.Reject(HelperLog.AppIds.Network, HelperLog.Subcategories.Campaign, nameof(EnsureDirectoryUnderRoot), "create above root");
            throw new ArgumentException("Campaign directories cannot be created above the campaign root.", nameof(path));
        }

        Directory.CreateDirectory(dir);
    }
}
