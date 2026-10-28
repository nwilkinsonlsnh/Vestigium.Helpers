namespace Vestigium.Helpers.Network;

public static partial class NetworkHelper
{
    public static IReadOnlyList<NetworkLmHostEntry> GetLmHosts()
        => NetworkLmHost.Read();
}
