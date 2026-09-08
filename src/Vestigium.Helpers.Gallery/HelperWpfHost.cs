using System.Windows;
using Vestigium.Helpers;
using Vestigium.Logging;

namespace Vestigium.Helpers.Gallery;

/// <summary>
/// Initializes Vestigium.Logging for a Helpers WPF gallery. Libraries still never call Initialize.
/// Galleries persist Debug enter/argument lines so the JSONL tab can reconstruct the chain.
/// </summary>
public static class HelperWpfHost
{
    public static void Start(Application app, string appId)
    {
        ArgumentNullException.ThrowIfNull(app);
        HelperLog.InitializeHost(appId, cfg =>
        {
            cfg.MinimumDiskLevel = VestigiumLogLevel.Debug;
        }, wpfApplication: app);
        app.Exit += (_, _) =>
        {
            HelperLog.Flush();
            HelperLog.Shutdown();
        };
        HelperLog.Information(
            appId,
            VestigiumStatus.Pending,
            appId,
            $"WPF gallery started APPID={appId} diskFloor=Debug");
    }
}