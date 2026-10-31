namespace Vestigium.Helpers.Network;

public static partial class NetworkHelper
{
    public static PortSet PortsFromList(IEnumerable<int>? ports)
        => PortSet.FromList(ports);

    public static PortSet PortsFromRange(int begin, int end)
        => PortSet.FromRange(begin, end);

    public static PortSet PortsWellKnown(string name)
        => PortSet.WellKnown(name);
}
