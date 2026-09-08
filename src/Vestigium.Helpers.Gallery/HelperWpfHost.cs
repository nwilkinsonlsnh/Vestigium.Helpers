using System.Windows;
using Vestigium.Helpers;

namespace Vestigium.Helpers.Gallery;

/// <summary>
/// Initializes Vestigium.Logging for a Helpers WPF gallery. Libraries still never call Initialize.
/// </summary>
public static class HelperWpfHost
{
    public static void Start(Application app, string appId)
    {
        ArgumentNullException.ThrowIfNull(app);
        HelperLog.InitializeHost(appId, wpfApplication: app);
        app.Exit += (_, _) =>
        {
            HelperLog.Flush();
            HelperLog.Shutdown();
        };
        HelperLog.Information(
            appId,
            Vestigium.Logging.VestigiumStatus.Pending,
            appId,
            $"WPF gallery started APPID={appId}");
    }
}
