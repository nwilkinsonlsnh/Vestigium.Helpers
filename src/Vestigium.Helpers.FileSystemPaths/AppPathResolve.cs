namespace Vestigium.Helpers.FileSystemPaths;

public static class AppPathResolve
{
    public static string From(string name, string? pathValue, string scope)
    {
        var file = Path.GetFileName(name.Trim());
        if (string.IsNullOrWhiteSpace(file))
            return "";

        var fromValue = FirstHit(pathValue, file);
        if (fromValue.Length > 0)
            return fromValue;

        foreach (var dir in SearchDirs(scope))
        {
            var candidate = Path.Combine(dir, file);
            if (File.Exists(candidate))
                return candidate;
        }
        return "";
    }

    private static string FirstHit(string? pathValue, string file)
    {
        if (string.IsNullOrWhiteSpace(pathValue))
            return "";
        foreach (var part in PathResolve.Clean(pathValue).Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var candidate = Path.Combine(part, file);
            if (File.Exists(candidate))
                return candidate;
        }
        return "";
    }

    private static IEnumerable<string> SearchDirs(string scope)
    {
        var system = Environment.GetFolderPath(Environment.SpecialFolder.System);
        var windows = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
        var wow = Path.Combine(windows, "SysWOW64");
        if (scope == "Wow")
        {
            yield return wow;
            yield return system;
        }
        else
        {
            yield return system;
            yield return wow;
        }
        foreach (var dir in (Environment.GetEnvironmentVariable("PATH") ?? "").Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            yield return dir;
    }
}
