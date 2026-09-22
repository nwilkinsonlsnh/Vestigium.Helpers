using Vestigium.Helpers;

namespace Vestigium.Helpers.WinReg;

internal static class RegistryPath
{
    public static string Normalize(string? key)
    {
        var text = key ?? string.Empty;
        if (text.Contains('/'))
        {
            HelperLog.Reject("key contains /");
            throw new ArgumentException("Registry key paths use backslash, not slash.", nameof(key));
        }

        return text.Trim().TrimStart('\\');
    }

    public static string Leaf(string path)
    {
        if (string.IsNullOrEmpty(path))
            return string.Empty;
        var i = path.LastIndexOf('\\');
        return i < 0 ? path : path[(i + 1)..];
    }

    public static string? NormalizeMachine(string? machine)
    {
        if (string.IsNullOrWhiteSpace(machine))
            return null;
        var text = machine.Trim().TrimStart('\\');
        if (text is "." or "localhost" || text.Equals(Environment.MachineName, StringComparison.OrdinalIgnoreCase))
            return null;
        return text;
    }
}
