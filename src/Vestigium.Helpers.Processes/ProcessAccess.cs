using System.Diagnostics;

namespace Vestigium.Helpers.Processes;

internal static class ProcessAccess
{
    internal static Availability Of(Process process)
    {
        try { return process.HasExited ? Availability.Gone : Availability.Denied; }
        catch { return Availability.Denied; }
    }
}
