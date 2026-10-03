namespace Vestigium.Helpers.Network;

public static partial class NetworkHelper
{
    public static IReadOnlyList<NetworkNetBiosName> GetNetBiosNames()
        => NetworkNetBios.Read();
}
