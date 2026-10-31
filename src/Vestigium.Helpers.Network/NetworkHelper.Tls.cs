namespace Vestigium.Helpers.Network;

public static partial class NetworkHelper
{
    public static NetworkJob<TlsProbeResult> ProbeTls(string host, int port, TlsProbeOptions? options = null)
        => TlsProbeEngine.Create(host, port, options);
}
