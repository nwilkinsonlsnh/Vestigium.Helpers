using Vestigium.Helpers;
using Vestigium.Logging;
namespace Vestigium.Helpers.Xml;

/// <summary>
/// XML document and serialization helpers.
/// </summary>
public static class XmlHelper
{
    public static string Identity => "Vestigium.Helpers.Xml";

    public static string Probe()
    {
        var app = HelperLog.AppIds.Xml;
        HelperLog.Information(app, VestigiumStatus.Pending, app, "Building a demo XML document.");
        HelperLog.Information(app, VestigiumStatus.Success, app, "XML probe complete. Identity=" + Identity);
        return Identity;
    }
}
