namespace Vestigium.Helpers.Network;

public static partial class NetworkHelper
{
    public static NetworkJob<SnmpSysDescrResult> ProbeSnmpSysDescr(string host, SnmpSysDescrOptions? options = null)
        => SnmpSysDescrEngine.Create(host, options);
}
