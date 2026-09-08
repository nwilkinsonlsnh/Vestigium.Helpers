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

    public static IReadOnlyList<string> TableStyles => ExcelTableStyles.Ids;

    public static string Probe()
    {
        var app = HelperLog.AppIds.ClosedXml;
        using var _ = HelperLog.Begin(app, HelperLog.Subcategories.Probe, "Probe");
        HelperLog.Information(app, VestigiumStatus.Pending, app, "Creating a demo workbook session.");
        using var book = Create("Probe", app);
        book.Sheet("Probe").WriteTable(
            SheetTable.Create(["Metric", "Value"], [["Identity", Identity]]),
            new SheetWriteOptions { CreateExcelTable = false, Autosize = false });
        HelperLog.Information(app, VestigiumStatus.Success, app, "Workbook session ready. Identity=" + Identity);
        HelperLog.Exit(app, HelperLog.Subcategories.Probe, "Probe", $"session={book.SessionId}");
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
        var app = string.IsNullOrWhiteSpace(appId) ? HelperLog.AppIds.ClosedXml : appId.Trim();
        var sessionId = HelperLog.NewId();
        using var _ = HelperLog.Begin(app, HelperLog.Subcategories.Session, "Create", $"firstSheet={firstSheetName ?? "(default)"} session={sessionId}", sessionId);
        HelperLog.Information(app, VestigiumStatus.Pending, HelperLog.Subcategories.Session, $"Creating a blank workbook session={sessionId}");
        var wb = new XLWorkbook();
        var session = new WorkbookSession(wb, path: null, app, sessionId);
        if (!string.IsNullOrWhiteSpace(firstSheetName))
        {
            var safe = ExcelNames.Sanitize(firstSheetName);
            if (session.SheetNames.Count == 1 && session.SheetNames[0] != safe)
            {
                wb.Worksheet(1).Name = safe;
            }
            else
            {
                session.Sheet(safe);
            }
        }

        return session;
    }

    public static WorkbookSession Open(string path, string? appId = null)
    {
        var app = string.IsNullOrWhiteSpace(appId) ? HelperLog.AppIds.ClosedXml : appId.Trim();
        var sessionId = HelperLog.NewId();
        using var _ = HelperLog.Begin(app, HelperLog.Subcategories.Session, "Open", $"path={path} session={sessionId}", sessionId);
        var target = HelperGuard.FileExists(path, nameof(path));
        HelperLog.Information(app, VestigiumStatus.Pending, HelperLog.Subcategories.Session, $"Opening workbook path={target} session={sessionId}");
        return new WorkbookSession(new XLWorkbook(target), target, app, sessionId);
    }

    /// <summary>
    /// Open a caller-supplied letterhead. Same as <see cref="Open"/> — this is not a token
    /// template engine. Fill with <see cref="WorkbookSession.WriteNamedRange"/> or a reserved sheet.
    /// </summary>
    public static WorkbookSession OpenTemplate(string path, string? appId = null)
        => Open(path, appId);

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
        int? populationSize = null,
        string? tableStyle = null)
    {
        HelperGuard.NotNull(book, nameof(book));
        HelperGuard.NotNull(series, nameof(series));
        using var _ = book.Trace(
            HelperLog.Subcategories.Session,
            "WriteSeries",
            $"series={series.SeriesId} n={series.Count} name={series.Name ?? "(none)"} prefix={prefix ?? "(none)"}");
        if (tableStyle is not null)
            book.TableStyle = tableStyle;
        SeriesWorkbook.Write(book, series, prefix, populationSize);
        HelperLog.Information(
            book.AppId,
            VestigiumStatus.Success,
            HelperLog.Subcategories.Session,
            $"WriteSeries series={series.SeriesId} n={series.Count} sheets={book.SheetNames.Count} session={book.SessionId}");
    }

    public static void Merge(WorkbookSession target, WorkbookSession source)
    {
        HelperGuard.NotNull(target, nameof(target));
        target.Merge(source);
    }
}
