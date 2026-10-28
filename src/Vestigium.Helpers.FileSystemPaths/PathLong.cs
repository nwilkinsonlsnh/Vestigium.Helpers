namespace Vestigium.Helpers.FileSystemPaths;

public static class PathLong
{
    public const int ClassicLimit = 260;

    public static string Apply(string? path)
    {
        if (string.IsNullOrEmpty(path))
            return "";
        if (path.StartsWith(@"\\?\", StringComparison.Ordinal))
            return path;
        if (path.Length <= ClassicLimit)
            return path;
        if (path.StartsWith(@"\\", StringComparison.Ordinal))
            return @"\\?\UNC\" + path[2..];
        return @"\\?\" + path;
    }
}
