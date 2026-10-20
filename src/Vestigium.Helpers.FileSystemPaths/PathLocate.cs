namespace Vestigium.Helpers.FileSystemPaths;

public static class PathLocate
{
    public static bool Exists(string? raw)
    {
        var path = PathResolve.Clean(raw);
        if (path.Length == 0)
            return false;
        if (File.Exists(path))
            return true;
        var redirected = Wow64(path);
        if (redirected.Length > 0 && File.Exists(redirected))
            return true;
        return InWinSxS(Path.GetFileName(path));
    }

    public static string Wow64(string path)
    {
        var windows = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
        var system32 = Path.Combine(windows, "System32");
        var wow = Path.Combine(windows, "SysWOW64");
        if (path.StartsWith(system32, StringComparison.OrdinalIgnoreCase))
            return wow + path[system32.Length..];
        return "";
    }

    private static bool InWinSxS(string file)
    {
        if (string.IsNullOrWhiteSpace(file))
            return false;
        var sxs = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "WinSxS");
        if (!Directory.Exists(sxs))
            return false;
        try
        {
            return Directory.EnumerateFiles(sxs, file, SearchOption.AllDirectories).Any();
        }
        catch (Exception)
        {
            return false;
        }
    }
}
