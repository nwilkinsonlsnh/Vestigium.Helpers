using Microsoft.Diagnostics.Tracing.Session;

namespace Vestigium.Helpers.Watch.Dns;

internal static class DnsClientSession
{
    public static async Task RunAsync(WatchPipe pipe, WatchRollup rollup, WatchClock clock, CancellationToken token)
    {
        using var session = new TraceEventSession("Vestigium-Watch-Dns-" + Guid.NewGuid().ToString("N"));
        session.EnableProvider(ResolverWatch.ProviderId);
        session.Source.Dynamic.All += data =>
        {
            if ((int)data.ID != ResolverWatch.QueryCompleted)
                return;

            var row = ResolverWatch.Map(
                (int)data.ID,
                data.ProcessID,
                Payload(data, "QueryName"),
                Payload(data, "QueryType"),
                Payload(data, "QueryStatus"),
                Payload(data, "QueryResults"));
            if (row is null)
                return;

            var rolled = rollup.Add(row.Name, row.Type, WatchSource.Event, row.Pid);
            if (rolled is null)
                return;

            pipe.WriteAsync(rolled, CancellationToken.None).GetAwaiter().GetResult();
        };

        using var stop = token.Register(() => session.Stop());
        var processing = Task.Run(() => session.Source.Process(), token);
        await Task.WhenAny(processing, clock.Completion).ConfigureAwait(false);
        session.Stop();
        await processing.ConfigureAwait(false);
    }

    private static string? Payload(Microsoft.Diagnostics.Tracing.TraceEvent data, string name)
    {
        try
        {
            return data.PayloadByName(name)?.ToString();
        }
        catch (Exception)
        {
            return null;
        }
    }
}
