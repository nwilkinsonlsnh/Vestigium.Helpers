namespace Vestigium.Helpers.Watch.Dns;

public static class Unseen
{
    public const int PipeFailed = 4;

    public const string Resolver = "Raw sockets and non-Windows DoH are not in this watch.";

    public const string Packet = "Outbound port 53 is in this watch. Browser Secure DNS is not.";

    public const string Both = "Outbound port 53 and the Windows resolver are in this watch. Browser Secure DNS is not.";

    public static string Text(WatchSource source)
        => source switch
        {
            WatchSource.Port => Packet,
            WatchSource.Both => Both,
            _ => Resolver
        };

    public static string Text(bool packet)
        => packet ? Packet : Resolver;

    public static WatchRow Line(WatchSource source)
        => new(DateTimeOffset.UtcNow, "", 0, "", "", "Unseen", Text(source), source.ToString().ToLowerInvariant());

    public static WatchRow Line(bool packet)
        => Line(packet ? WatchSource.Port : WatchSource.Event);

    public static int OpenPipe(string name, out WatchPipe? pipe)
    {
        try
        {
            pipe = WatchPipe.Create(name);
            return 0;
        }
        catch (Exception)
        {
            pipe = null;
            return PipeFailed;
        }
    }
}
