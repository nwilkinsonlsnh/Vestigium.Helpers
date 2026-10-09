using System.Security.Principal;

namespace Vestigium.Helpers.Watch.Dns;

public static class Program
{
    public const int NotElevated = 2;

    public static async Task<int> Main(string[] args)
    {
        if (!Elevation.IsElevated())
            return NotElevated;

        var packet = PacketWatch.Requested(args);
        var name = "Vestigium.Watch.Dns." + Guid.NewGuid().ToString("N");
        var opened = Unseen.OpenPipe(name, out var pipe);
        if (opened != 0 || pipe is null)
            return Unseen.PipeFailed;

        await using var open = pipe;
        if (!WatchClock.TryCreate(null, out var clock, out _))
            return 1;

        try
        {
            await open.WriteAsync(Unseen.Line(packet), CancellationToken.None).ConfigureAwait(false);
            var run = packet
                ? PacketWatch.RunAsync(open, clock!, CancellationToken.None)
                : ResolverWatch.RunAsync(open, clock!, ResolverWatch.StartSessionAsync, CancellationToken.None);
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
