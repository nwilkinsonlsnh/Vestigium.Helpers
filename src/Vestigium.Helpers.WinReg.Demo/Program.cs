using Vestigium.Helpers;
using Vestigium.Helpers.WinReg;

return HelperDemoHost.Run(
    HelperLog.AppIds.WinReg,
    RegistryHelper.Identity,
    static () => RegistryHelper.Probe());
