using Vestigium.Helpers;

return HelperDemoHost.Run(
    HelperLog.AppIds.Core,
    HelperGuard.Identity,
    static () => HelperGuard.Probe());
