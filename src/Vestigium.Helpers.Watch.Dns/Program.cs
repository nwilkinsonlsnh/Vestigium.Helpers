using System.Security.Principal;

namespace Vestigium.Helpers.Watch.Dns;

public static class Program
{
    public const int NotElevated = 2;

    public static async Task<int> Main(string[] args)
    {
        if (!Elevation.IsElevated())
            return NotElevated;

        if (!WatchRequest.ParseArgs(args, out var request, out _))
            return 1;

        var name = string.IsNullOrWhiteSpace(request!.PipeName)
            ? "Vestigium.Watch.Dns." + Guid.NewGuid().ToString("N")
            : request.PipeName;
        var opened = Unseen.OpenPipe(name, out var pipe);
        if (opened != 0 || pipe is null)
            return Unseen.PipeFailed;

        await using var open = pipe;
        try
        {
            using var clientWait = new CancellationTokenSource(TimeSpan.FromSeconds(request!.Clock.Seconds));
            await open.WaitForClientAsync(clientWait.Token).ConfigureAwait(false);
            await open.WriteAsync(Unseen.Line(request.Source), CancellationToken.None).ConfigureAwait(false);
            return await WatchRun.RunAsync(open, request, CancellationToken.None).ConfigureAwait(false);
        }
        finally
        {
            request.Clock.Stop();
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
