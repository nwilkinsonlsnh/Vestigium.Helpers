using Vestigium.Helpers;
using Vestigium.Logging;
namespace Vestigium.Helpers.ClosedXml;

/// <summary>
/// ClosedXML wrappers for reading and writing Excel workbooks.
/// </summary>
public static class WorkbookHelper
{
    public static string Identity => "Vestigium.Helpers.ClosedXml";

    public static string Probe()
    {
        var app = HelperLog.AppIds.ClosedXml;
        HelperLog.Information(app, VestigiumStatus.Pending, app, "Creating a demo workbook session.");
        HelperLog.Information(app, VestigiumStatus.Success, app, "Workbook session ready. Identity=" + Identity);
        return Identity;
    }
}
