using Vestigium.Helpers;
using Vestigium.Logging;

namespace Vestigium.Helpers.Network;

/// <summary>
/// Only logging door for this library. Category <c>Helpers</c>, APPID <c>Network</c>.
/// Writes go through <see cref="HelperLog"/> into the sibling
/// <c>Vestigium.Logging</c> project (JSON Lines). The library never calls
/// <c>VestigiumLogger.Initialize</c>. Hosts (Network.Demo, PingIQ, ProbeHost) initialize.
/// Never packet payloads, WLAN keys, or <see cref="Exception"/> objects.
/// </summary>
internal static class NetworkLog
{
    internal const string App = HelperLog.AppIds.Network;

    public static IDisposable Begin(string subcategory, string method, string? detail = null, string? correlationId = null)
        => HelperLog.Begin(App, subcategory, method, detail, correlationId);

    public static void Pending(string subcategory, string message)
        => HelperLog.Information(App, VestigiumStatus.Pending, subcategory, message);

    public static void Success(string subcategory, string message)
        => HelperLog.Information(App, VestigiumStatus.Success, subcategory, message);

    public static void Warning(string subcategory, string message)
        => HelperLog.Warning(App, VestigiumStatus.None, subcategory, message);

    public static void Failed(string subcategory, string message)
        => HelperLog.Error(App, VestigiumStatus.Failed, subcategory, message);
}
