using Vestigium.Helpers;
using Vestigium.Helpers.Services;

return HelperDemoHost.Run(
    HelperLog.AppIds.Services,
    ServiceHelper.Identity,
    static () => ServiceHelper.Probe());
