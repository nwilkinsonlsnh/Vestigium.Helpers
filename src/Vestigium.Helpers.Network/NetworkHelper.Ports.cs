namespace Vestigium.Helpers.Network;

public static partial class NetworkHelper
{
    public static bool TryPort(int port, out NetworkPortGuess guess)
        => NetworkPorts.TryByPort(port, out guess);

    public static IReadOnlyList<NetworkPortGuess> PortsFor(string name)
        => NetworkPorts.ByName(name);

    public static bool TryService(string? protocol, int port, out string name)
    {
        if (!NetworkPorts.Try(protocol, port, out var guess))
        {
            name = string.Empty;
            return false;
        }

        name = guess.Name;
        return true;
    }
}
