using Vestigium.Logging;

namespace Vestigium.Helpers.Network;

/// <summary>
/// Only logging door for this library. Category Helpers, APPID Network.
/// Hosts initialize VestigiumLogger. Never packet payloads, WLAN keys, or Exception objects.
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
        => HelperLog.Warning(App, VestigiumStatus.Warning, subcategory, message);

    public static void Failed(string subcategory, string message)
        => HelperLog.Error(App, VestigiumStatus.Failed, subcategory, message);
}
