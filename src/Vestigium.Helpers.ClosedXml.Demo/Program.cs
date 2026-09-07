using Vestigium.Helpers;
using Vestigium.Helpers.ClosedXml;

return HelperDemoHost.Run(
    HelperLog.AppIds.ClosedXml,
    WorkbookHelper.Identity,
    static () => WorkbookHelper.Probe());
