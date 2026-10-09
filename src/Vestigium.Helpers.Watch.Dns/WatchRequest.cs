namespace Vestigium.Helpers.Watch.Dns;

public enum WatchSource
{
    Event,
    Port,
    Both
}

public sealed class WatchRequest
{
    private WatchRequest(WatchSource source, WatchClock clock)
    {
        Source = source;
        Clock = clock;
    }

    public WatchSource Source { get; }

    public WatchClock Clock { get; }

    public string? PipeName { get; private set; }

    public static bool TryCreate(WatchSource? source, int? seconds, out WatchRequest? request, out string? reject)
    {
        if (source is null)
        {
            request = null;
            reject = "Source is required. Event, Port, or Both.";
            return false;
        }

        if (!WatchClock.TryCreate(seconds, out var clock, out reject))
        {
            request = null;
            return false;
        }

        request = new WatchRequest(source.Value, clock!);
        return true;
    }

    public static bool ParseArgs(string[] args, out WatchRequest? request, out string? reject)
    {
        WatchSource? source = null;
        int? seconds = null;
        string? pipe = null;
        foreach (var arg in args)
        {
            if (arg.StartsWith("pipe:", StringComparison.OrdinalIgnoreCase))
            {
                pipe = arg[5..];
                if (string.IsNullOrWhiteSpace(pipe))
                {
                    request = null;
                    reject = "Pipe name is required.";
                    return false;
                }
                continue;
            }

            if (int.TryParse(arg, out var value))
            {
                seconds = value;
                continue;
            }

            if (source is not null || !TrySource(arg, out var parsed))
            {
                request = null;
                reject = "Source must be Event, Port, or Both.";
                return false;
            }

            source = parsed;
        }

        if (!TryCreate(source ?? WatchSource.Both, seconds, out request, out reject))
            return false;
        request!.PipeName = pipe;
        return true;
    }

    public static bool TrySource(string value, out WatchSource source)
    {
        if (string.Equals(value, "event", StringComparison.OrdinalIgnoreCase)
            || string.Equals(value, "resolver", StringComparison.OrdinalIgnoreCase))
        {
            source = WatchSource.Event;
            return true;
        }

        if (string.Equals(value, "port", StringComparison.OrdinalIgnoreCase)
            || string.Equals(value, "packet", StringComparison.OrdinalIgnoreCase))
        {
            source = WatchSource.Port;
            return true;
        }

        if (string.Equals(value, "both", StringComparison.OrdinalIgnoreCase))
        {
            source = WatchSource.Both;
            return true;
        }

        source = default;
        return false;
    }
}
