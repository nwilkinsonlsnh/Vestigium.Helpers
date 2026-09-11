namespace Vestigium.Helpers.Processes;

internal static class ProcessImagePath
{
    internal static string Normalize(string? imagePath, string name)
    {
        if (string.IsNullOrWhiteSpace(imagePath))
            return name;
        var path = imagePath.Trim();
        path = Swap(path, "SysWOW64", "System32");
        path = Swap(path, "Sysnative", "System32");
        return path;
    }

    private static string Swap(string path, string from, string to)
    {
        var needle = Path.DirectorySeparatorChar + from + Path.DirectorySeparatorChar;
        var alt = Path.AltDirectorySeparatorChar + from + Path.AltDirectorySeparatorChar;
        var replacement = Path.DirectorySeparatorChar + to + Path.DirectorySeparatorChar;
        return path
            .Replace(needle, replacement, StringComparison.OrdinalIgnoreCase)
            .Replace(alt, replacement, StringComparison.OrdinalIgnoreCase);
    }
}
