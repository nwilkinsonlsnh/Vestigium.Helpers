using Vestigium.Helpers;
using Vestigium.Helpers.Analytics;

return HelperDemoHost.Run(
    HelperLog.AppIds.Analytics,
    AnalyticsHelper.Identity,
    static () => AnalyticsHelper.Probe());
