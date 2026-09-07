using Vestigium.Helpers;
using Vestigium.Helpers.Json;

return HelperDemoHost.Run(
    HelperLog.AppIds.Json,
    JsonHelper.Identity,
    static () => JsonHelper.Probe());
