using System.ComponentModel;
using System.Diagnostics;
using Vestigium.Helpers;
using Vestigium.Logging;

namespace Vestigium.Helpers.Processes;

internal static class ProcessKiller
{
    private static readonly HashSet<string> ProtectedNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "csrss", "csrss.exe",
        "smss", "smss.exe",
        "wininit", "wininit.exe",
        "winlogon", "winlogon.exe",
        "services", "services.exe",
        "lsass", "lsass.exe",
        "System", "Registry", "Idle",
        "Secure System", "Memory Compression"
    };

    internal static bool IsGuarded(ProcessInfo row)
    {
        if (ProtectedNames.Contains(row.Name) || ProtectedNames.Contains(Path.GetFileNameWithoutExtension(row.Name)))
            return true;
        if (row.IntegrityLevel == IntegrityLevel.Protected)
            return true;
        if (row.Protection is { } protection
            && !string.IsNullOrWhiteSpace(protection.Level)
            && !string.Equals(protection.Level, "None", StringComparison.OrdinalIgnoreCase))
            return true;
        return false;
    }

    internal static ProcessKillResult Kill(int pid, bool force)
    {
        HelperGuard.InRange(pid, 1, nameof(pid));
        var row = ProcessSnapshotter.CapturePid(pid, ProcessDetailLevel.Identity);
        if (row is null)
            return Finish(pid, ProcessKillStatus.Gone, force, "gone");

        if (IsGuarded(row))
            return Finish(pid, ProcessKillStatus.Denied, force, "protected " + row.Name);

        try
        {
            using var process = Process.GetProcessById(pid);
            if (force)
                process.Kill(entireProcessTree: false);
            else if (!process.CloseMainWindow())
                return Finish(pid, ProcessKillStatus.Failed, force, "no window");
            return Finish(pid, ProcessKillStatus.Ok, force, null);
        }
        catch (ArgumentException) { return Finish(pid, ProcessKillStatus.Gone, force, "gone"); }
        catch (InvalidOperationException) { return Finish(pid, ProcessKillStatus.Gone, force, "gone"); }
        catch (Win32Exception ex) when (ex.NativeErrorCode == 5)
        {
            return Finish(pid, ProcessKillStatus.Denied, force, "access denied");
        }
        catch (Exception ex)
        {
            return Finish(pid, ProcessKillStatus.Failed, force, ex.Message);
        }
    }

    internal static IReadOnlyList<ProcessKillResult> KillTree(int pid, bool force)
    {
        HelperGuard.InRange(pid, 1, nameof(pid));
        var descendants = ProcessTreeWalker.DescendantsOf(pid).OrderByDescending(row => row.Pid).ToArray();
        var results = new List<ProcessKillResult>(descendants.Length + 1);
        foreach (var child in descendants)
        {
            if (child.AmbiguousParent)
            {
                results.Add(Finish(child.Pid, ProcessKillStatus.Denied, force, "ambiguous parent"));
                continue;
            }

            results.Add(Kill(child.Pid, force));
        }

        results.Add(Kill(pid, force));
        return results;
    }

    internal static IReadOnlyList<ProcessKillResult> KillSearch(ProcessSearchRequest search, KillConfirm confirm)
    {
        ArgumentNullException.ThrowIfNull(search);
        ArgumentNullException.ThrowIfNull(confirm);
        if (!confirm.Confirm)
        {
            HelperLog.Reject("KillSearch Confirm=false");
            throw new InvalidOperationException("KillSearch requires KillConfirm.Confirm = true.");
        }

        HelperGuard.InRange(confirm.MaxResults, 1, nameof(confirm.MaxResults));
        HelperGuard.Require(confirm.MaxResults <= KillConfirm.Cap, nameof(confirm.MaxResults), "KillSearch cap is 16.");
        var hits = ProcessHelper.Search(search.Term, search.Mode, search.Fields, ProcessDetailLevel.Identity, confirm.MaxResults);
        return hits.Select(row => Kill(row.Pid, force: true)).ToArray();
    }

    private static ProcessKillResult Finish(int pid, ProcessKillStatus status, bool force, string? message)
    {
        var line = $"Kill pid={pid} force={force} status={status}";
        if (message is not null) line += " " + message;
        HelperLog.Information(
            HelperLog.AppIds.Processes,
            status == ProcessKillStatus.Ok ? VestigiumStatus.Success : VestigiumStatus.Failed,
            HelperLog.Subcategories.Inventory,
            line);
        return new ProcessKillResult { Pid = pid, Status = status, Message = message };
    }
}
