using System.Collections.Concurrent;

namespace Vestigium.Helpers.WinReg;

public sealed partial class RegistryJournal
{
    private static readonly ConcurrentDictionary<string, byte> OpenFiles = new(StringComparer.OrdinalIgnoreCase);

    internal static void Track(string path) => OpenFiles[System.IO.Path.GetFullPath(path)] = 1;

    internal static void Untrack(string path) => OpenFiles.TryRemove(System.IO.Path.GetFullPath(path), out _);

    public static bool IsOpen(string path)
        => OpenFiles.ContainsKey(System.IO.Path.GetFullPath(path));
}
