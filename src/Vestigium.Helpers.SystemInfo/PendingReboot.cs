using Microsoft.Win32;

namespace Vestigium.Helpers.SystemInfo;

public sealed record PendingRebootState(bool Pending, IReadOnlyList<string> Renames)
{
    public static PendingRebootState Read()
    {
        var renames = new List<string>();
        var pending = false;
        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\Session Manager");
            var value = key?.GetValue("PendingFileRenameOperations");
            if (value is string[] lines && lines.Length > 0)
            {
                pending = true;
                renames.AddRange(lines.Where(line => !string.IsNullOrWhiteSpace(line)));
            }
            else if (value is string text && text.Length > 0)
            {
                pending = true;
                renames.AddRange(text.Split(['\r', '\n', '\0'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
            }
        }
        catch (Exception)
        {
        }

        pending |= Exists(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Component Based Servicing\RebootPending");
        pending |= Exists(@"SOFTWARE\Microsoft\Windows\CurrentVersion\WindowsUpdate\Auto Update\RebootRequired");
        return new PendingRebootState(pending, renames);
    }

    private static bool Exists(string path)
    {
        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(path);
            return key is not null;
        }
        catch (Exception)
        {
            return false;
        }
    }
}
