using Vestigium.Helpers.Network;

namespace Vestigium.Helpers.Network.RawSocket;

public static class RawSocketHelper
{
    public static RawSocketJob<SynScanResult> SynScan(IEnumerable<string> hosts, PortSet ports, SynScanOptions? options = null)
        => SynScanEngine.Create(hosts, ports, options);

    public static RawSocketJob<SynScanResult> SynScan(string host, PortSet ports, SynScanOptions? options = null)
        => SynScan([host], ports, options);
}
