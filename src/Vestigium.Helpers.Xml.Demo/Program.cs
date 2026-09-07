using Vestigium.Helpers;
using Vestigium.Helpers.Xml;

return HelperDemoHost.Run(
    HelperLog.AppIds.Xml,
    XmlHelper.Identity,
    static () => XmlHelper.Probe());
