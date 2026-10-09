using System.Security.Principal;

namespace Vestigium.Helpers.Watch.Dns;

public static class Program
{
    public const int NotElevated = 2;

    public static async Task<int> Main(string[] args)
    {
        if (!Elevation.IsElevated())
            return NotElevated;

        var name = "Vestigium.Watch.Dns." + Guid.NewGuid().ToString("N");
        await using var pipe = WatchPipe.Create(name);
        if (!WatchClock.TryCreate(null, out var clock, out _))
            return 1;

        try
        {
            var run = PacketWatch.Requested(args)
                ? PacketWatch.RunAsync(pipe, clock!, CancellationToken.None)
                : ResolverWatch.RunAsync(pipe, clock!, ResolverWatch.StartSessionAsync, CancellationToken.None);
            return await run.ConfigureAwait(false);
        }
        finally
        {
            clock!.Stop();
        }
    }
}

public static class Elevation
{
    public static bool IsElevated()
    {
        if (!OperatingSystem.IsWindows())
            return false;

        using var identity = WindowsIdentity.GetCurrent();
        return new WindowsPrincipal(identity).IsInRole(WindowsBuiltInRole.Administrator);
    }
}
