namespace Vestigium.Helpers.Network;

public static partial class NetworkHelper
{
    public static bool TryPort(int port, out NetworkPortGuess guess)
        => NetworkPorts.TryByPort(port, out guess);

    public static IReadOnlyList<NetworkPortGuess> PortsFor(string name)
        => NetworkPorts.ByName(name);
}
