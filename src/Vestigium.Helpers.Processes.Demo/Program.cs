using Vestigium.Helpers;
using Vestigium.Helpers.Processes;

return HelperDemoHost.Run(
    HelperLog.AppIds.Processes,
    ProcessHelper.Identity,
    static () => ProcessHelper.Probe());
