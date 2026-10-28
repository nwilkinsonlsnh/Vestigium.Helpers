namespace Vestigium.Helpers.Watch.Dns;

public static class WatchRun
{
    public static Task<int> RunAsync(WatchPipe pipe, WatchRequest request, CancellationToken token)
    {
        var rollup = new WatchRollup();
        return RunAsync(
            pipe,
            request,
            (open, clock, token) => ResolverWatch.StartSessionAsync(open, rollup, clock, token),
            (open, clock, token) => PacketWatch.RunAsync(open, rollup, clock, token),
            token);
    }

    public static async Task<int> RunAsync(
        WatchPipe pipe,
        WatchRequest request,
        Func<WatchPipe, WatchClock, CancellationToken, Task> startEvent,
        Func<WatchPipe, WatchClock, CancellationToken, Task<int>> startPort,
        CancellationToken token)
    {
        var tasks = new List<Task<int>>();
        if (request.Source is WatchSource.Event or WatchSource.Both)
            tasks.Add(RunOneAsync(pipe, request.Source, "resolver", () => startEvent(pipe, request.Clock, token)));
        if (request.Source is WatchSource.Port or WatchSource.Both)
            tasks.Add(RunOneAsync(pipe, request.Source, "packet", async () =>
            {
                var code = await startPort(pipe, request.Clock, token).ConfigureAwait(false);
                if (code != 0)
                    throw new InvalidOperationException("Port bind failed");
            }));

        var codes = await Task.WhenAll(tasks).ConfigureAwait(false);
        return codes.All(code => code != 0) ? ResolverWatch.SessionFailed : 0;
    }

    private static async Task<int> RunOneAsync(WatchPipe pipe, WatchSource requested, string mode, Func<Task> start)
    {
        try
        {
            await start().ConfigureAwait(false);
            return 0;
        }
        catch (Exception ex)
        {
            var message = string.IsNullOrWhiteSpace(ex.Message) ? "Session failed" : ex.Message.Trim();
            await pipe.WriteAsync(
                new WatchRow(DateTimeOffset.UtcNow, "", 0, "", "", "Failed", message, mode),
                CancellationToken.None).ConfigureAwait(false);
            return requested is WatchSource.Both ? 0 : ResolverWatch.SessionFailed;
        }
    }
}
