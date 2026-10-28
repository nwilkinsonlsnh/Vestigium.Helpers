namespace Vestigium.Helpers.Watch.Dns;

public static class ResolverWatch
{
    public static readonly Guid ProviderId = new("1C95126E-7EEA-49A9-A3FE-A378B03DDB4D");
    public const int QuerySent = 3006;
    public const int QueryCompleted = 3008;
    public const int SessionFailed = 3;

    public static WatchRow? Map(int eventId, int pid, string? name, string? type, string? status, string? results)
    {
        if (eventId != QueryCompleted && eventId != QuerySent)
            return null;

        return new WatchRow(
            DateTimeOffset.UtcNow,
            "",
            pid,
            name?.Trim() ?? "",
            type?.Trim() ?? "",
            status?.Trim() ?? "",
            results?.Trim() ?? "",
            "resolver");
    }

    public static async Task<int> RunAsync(
        WatchPipe pipe,
        WatchClock clock,
        Func<WatchPipe, WatchClock, CancellationToken, Task> start,
        CancellationToken token)
    {
        try
        {
            await start(pipe, clock, token).ConfigureAwait(false);
            return 0;
        }
        catch (Exception ex)
        {
            var message = string.IsNullOrWhiteSpace(ex.Message) ? "Session failed" : ex.Message.Trim();
            await pipe.WriteAsync(
                new WatchRow(DateTimeOffset.UtcNow, "", 0, "", "", "Failed", message, "resolver"),
                CancellationToken.None).ConfigureAwait(false);
            return SessionFailed;
        }
    }

    public static Task StartSessionAsync(WatchPipe pipe, WatchRollup rollup, WatchClock clock, CancellationToken token)
    {
        if (!OperatingSystem.IsWindows())
            throw new PlatformNotSupportedException("The DNS client session requires Windows.");

        return DnsClientSession.RunAsync(pipe, rollup, clock, token);
    }

    public static Task StartSessionAsync(WatchPipe pipe, WatchClock clock, CancellationToken token)
        => StartSessionAsync(pipe, new WatchRollup(), clock, token);

}
