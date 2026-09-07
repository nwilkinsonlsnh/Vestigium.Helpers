using Vestigium.Helpers;
using Vestigium.Helpers.Network;

return HelperDemoHost.Run(
    HelperLog.AppIds.Network,
    NetworkHelper.Identity,
    static () => NetworkHelper.Probe());
