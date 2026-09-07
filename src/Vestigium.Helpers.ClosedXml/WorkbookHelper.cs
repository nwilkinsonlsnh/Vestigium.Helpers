using ClosedXML.Excel;
using Vestigium.Helpers;
using Vestigium.Helpers.Analytics;
using Vestigium.Logging;

namespace Vestigium.Helpers.ClosedXml;

/// <summary>
/// ClosedXML wrappers for writing Excel workbooks. Libraries never call Initialize.
/// </summary>
public static class WorkbookHelper
{
    public static string Identity => "Vestigium.Helpers.ClosedXml";

    public static string Probe()
    {
        var app = HelperLog.AppIds.ClosedXml;
        HelperLog.Information(app, VestigiumStatus.Pending, app, "Creating a demo workbook session.");
        using var book = Create("Probe", app);
        book.Sheet("Probe").WriteTable(
            SheetTable.Create(["Metric", "Value"], [["Identity", Identity]]),
            new SheetWriteOptions { CreateExcelTable = false, Autosize = false });
        HelperLog.Information(app, VestigiumStatus.Success, app, "Workbook session ready. Identity=" + Identity);
        return Identity;
    }

    public static string DefaultExportDirectory(string appId)
    {
        var id = HelperGuard.NotBlank(appId, nameof(appId));
        var desktop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
        if (string.IsNullOrWhiteSpace(desktop))
            desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
        if (string.IsNullOrWhiteSpace(desktop))
        {
            var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            desktop = Path.Combine(string.IsNullOrWhiteSpace(home) ? "." : home, "Desktop");
        }

        return Path.Combine(desktop, "Vestigium", "Exports", id);
    }

    public static string NewExportPath(string appId, string? stem = null)
    {
        var id = HelperGuard.NotBlank(appId, nameof(appId));
        var stamp = DateTime.Now.ToString("yyyyMMdd-HHmmss");
        var file = string.IsNullOrWhiteSpace(stem)
            ? $"vestigium-{id}-{stamp}.xlsx"
            : $"{stem.Trim()}-{stamp}.xlsx";
        foreach (var ch in Path.GetInvalidFileNameChars())
            file = file.Replace(ch, '_');
        return Path.Combine(DefaultExportDirectory(id), file);
    }

    public static WorkbookSession Create(string? firstSheetName = null, string? appId = null)
    {
        HelperLog.Information(
            appId ?? HelperLog.AppIds.ClosedXml,
            VestigiumStatus.Pending,
            HelperLog.AppIds.ClosedXml,
            "Creating a blank workbook.");
        var wb = new XLWorkbook();
        var session = new WorkbookSession(wb, path: null, appId);
        if (!string.IsNullOrWhiteSpace(firstSheetName))
        {
            var safe = ExcelNames.Sanitize(firstSheetName);
            if (session.SheetNames.Count == 1 && session.SheetNames[0] != safe)
                wb.Worksheet(1).Name = safe;
            else
                session.Sheet(safe);
        }

        return session;
    }

    public static WorkbookSession Open(string path, string? appId = null)
    {
        var target = HelperGuard.NotBlank(path, nameof(path));
        if (!File.Exists(target))
        {
            HelperLog.Error(
                appId ?? HelperLog.AppIds.ClosedXml,
                VestigiumStatus.Failed,
                HelperLog.AppIds.ClosedXml,
                $"Open failed. File not found: {target}");
            throw new FileNotFoundException("Workbook not found.", target);
        }

        HelperLog.Information(
            appId ?? HelperLog.AppIds.ClosedXml,
            VestigiumStatus.Pending,
            HelperLog.AppIds.ClosedXml,
            $"Opening workbook {target}");
        return new WorkbookSession(new XLWorkbook(target), target, appId);
    }

    public static WorkbookSession OpenOrCreate(string path, string? firstSheetName = null, string? appId = null)
    {
        var target = HelperGuard.NotBlank(path, nameof(path));
        if (File.Exists(target))
            return Open(target, appId);

        var session = Create(firstSheetName, appId);
        session.SaveAs(target);
        return session;
    }

    public static void WriteSeries(
        WorkbookSession book,
        NumericSeries series,
        string? prefix = null,
        int? populationSize = null)
        => SeriesWorkbook.Write(book, series, prefix, populationSize);
}
