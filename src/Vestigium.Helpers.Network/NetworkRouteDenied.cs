namespace Vestigium.Helpers.Network;

public sealed class NetworkRouteDenied : InvalidOperationException
{
    public NetworkRouteDenied(string message)
        : base(message)
    {
    }

    public NetworkRouteDenied(string message, Exception inner)
        : base(message, inner)
    {
    }
}
