namespace Vestigium.Helpers.Network.RawSocket;

public enum RawSocketPrivilege
{
    Granted,
    Denied
}

public sealed record RawSocketAccess(RawSocketPrivilege Privilege, string Detail)
{
    public bool IsGranted => Privilege == RawSocketPrivilege.Granted;
}
