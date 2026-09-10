using Microsoft.Win32;

namespace Vestigium.Helpers.Processes;

internal static class ProcessAutostart
{
    internal static string? Locate(string? imagePath, string name)
    {
        var file = string.IsNullOrWhiteSpace(imagePath) ? name : imagePath;
        var shortName = Path.GetFileName(file);

        foreach (var hit in ReadRun(RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Run", "HKCU Run"))
            if (Matches(hit.Value, file, shortName))
                return hit.Label;
        foreach (var hit in ReadRun(RegistryHive.LocalMachine, @"Software\Microsoft\Windows\CurrentVersion\Run", "HKLM Run"))
            if (Matches(hit.Value, file, shortName))
                return hit.Label;
        foreach (var hit in ReadRun(RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\RunOnce", "HKCU RunOnce"))
            if (Matches(hit.Value, file, shortName))
                return hit.Label;
        foreach (var hit in ReadRun(RegistryHive.LocalMachine, @"Software\Microsoft\Windows\CurrentVersion\RunOnce", "HKLM RunOnce"))
            if (Matches(hit.Value, file, shortName))
                return hit.Label;

        var startup = Environment.GetFolderPath(Environment.SpecialFolder.Startup);
        if (!string.IsNullOrWhiteSpace(startup) && Directory.Exists(startup))
        {
            foreach (var path in Directory.EnumerateFiles(startup))
            {
                if (Matches(path, file, shortName) || Matches(Path.GetFileNameWithoutExtension(path), file, shortName))
                    return "Startup folder";
            }
        }

        return "None";
    }

    private static IEnumerable<(string Label, string Value)> ReadRun(RegistryHive hive, string key, string label)
    {
        RegistryKey? root = null;
        try
        {
            root = RegistryKey.OpenBaseKey(hive, RegistryView.Registry64).OpenSubKey(key)
                ?? RegistryKey.OpenBaseKey(hive, RegistryView.Registry32).OpenSubKey(key);
            if (root is null)
                yield break;
            foreach (var name in root.GetValueNames())
            {
                var value = root.GetValue(name)?.ToString();
                if (!string.IsNullOrWhiteSpace(value))
                    yield return (label + " / " + name, value);
            }
        }
        finally
        {
            root?.Dispose();
        }
    }

    private static bool Matches(string candidate, string imagePath, string shortName)
    {
        if (string.IsNullOrWhiteSpace(candidate))
            return false;
        return candidate.Contains(imagePath, StringComparison.OrdinalIgnoreCase)
            || candidate.Contains(shortName, StringComparison.OrdinalIgnoreCase);
    }
}
