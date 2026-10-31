namespace Vestigium.Helpers.Network;

public static partial class NetworkHelper
{
    public static NetworkJob<PortScanResult> ScanPorts(IEnumerable<string> hosts, PortSet ports, PortScanOptions? options = null)
        => PortScanEngine.Create(hosts, ports, options);

    public static NetworkJob<PortScanResult> ScanPorts(string host, PortSet ports, PortScanOptions? options = null)
        => ScanPorts([host], ports, options);
}
