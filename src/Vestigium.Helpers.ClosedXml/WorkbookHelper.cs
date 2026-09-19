using ClosedXML.Excel;
using Vestigium.Helpers.Analytics;
using Vestigium.Logging;

namespace Vestigium.Helpers.ClosedXml;

/// <summary>
/// ClosedXML wrappers. Logging goes through Vestigium.Logging (APPID ClosedXml, EVENTID 11000+).
/// This library never calls <see cref="VestigiumLogger.Initialize"/>.
/// </summary>
public static class WorkbookHelper
{
    public static string Identity => "Vestigium.Helpers.ClosedXml";

    public static IReadOnlyList<string> TableStyles => ExcelTableStyles.Ids;

    public static string Probe()
    {
        ClosedXmlLog.Debug(ClosedXmlEvents.ProbeEnter, ClosedXmlCatalog.Subcategories.Probe, "enter Probe");
        using var book = Create("Probe");
        book.Sheet("Probe").WriteTable(
            SheetTable.Create(["Metric", "Value"], [["Identity", Identity]]),
            new SheetWriteOptions { CreateExcelTable = false, Autosize = false });
        ClosedXmlLog.Information(
            ClosedXmlEvents.ProbeComplete,
            ClosedXmlCatalog.Subcategories.Probe,
            "probe complete",
            book.SessionId,
            ClosedXmlLog.Props(("identity", Identity)));
        return Identity;
    }

    public static string DefaultExportDirectory(string appId)
    {
        var id = ClosedXmlLog.RequireNotBlank(appId, nameof(appId), ClosedXmlEvents.SessionRejected, ClosedXmlCatalog.Subcategories.Session);
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
        var id = ClosedXmlLog.RequireNotBlank(appId, nameof(appId), ClosedXmlEvents.SessionRejected, ClosedXmlCatalog.Subcategories.Session);
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
        var app = string.IsNullOrWhiteSpace(appId) ? ClosedXmlCatalog.AppId : appId.Trim();
        var sessionId = ClosedXmlLog.NewId();
        ClosedXmlLog.Debug(
            ClosedXmlEvents.SessionEnter,
            ClosedXmlCatalog.Subcategories.Session,
            "enter session",
            sessionId,
            ClosedXmlLog.Props(("via", "Create"), ("firstSheet", firstSheetName)),
            app);
        try
        {
            var wb = new XLWorkbook();
            var session = new WorkbookSession(wb, path: null, app, sessionId);
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
        catch (Exception ex)
        {
            ClosedXmlLog.Unexpected(ClosedXmlEvents.SessionThrown, ClosedXmlCatalog.Subcategories.Session, ex, sessionId, app);
            throw;
        }
    }

    public static WorkbookSession Open(string path, string? appId = null)
    {
        var app = string.IsNullOrWhiteSpace(appId) ? ClosedXmlCatalog.AppId : appId.Trim();
        var sessionId = ClosedXmlLog.NewId();
        var target = ClosedXmlLog.RequireFile(path, nameof(path));
        ClosedXmlLog.Debug(
            ClosedXmlEvents.SessionEnter,
            ClosedXmlCatalog.Subcategories.Session,
            "enter session",
            sessionId,
            ClosedXmlLog.Props(("via", "Open"), ("path", target)),
            app);
        try
        {
            var session = new WorkbookSession(new XLWorkbook(target), target, app, sessionId);
            ClosedXmlLog.Information(
                ClosedXmlEvents.SessionOpened,
                ClosedXmlCatalog.Subcategories.Session,
                "session opened",
                sessionId,
                ClosedXmlLog.Props(("path", target)),
                app);
            return session;
        }
        catch (Exception ex)
        {
            ClosedXmlLog.Unexpected(ClosedXmlEvents.SessionThrown, ClosedXmlCatalog.Subcategories.Session, ex, sessionId, app);
            throw;
        }
    }

    public static WorkbookSession OpenTemplate(string path, string? appId = null)
        => Open(path, appId);

    public static WorkbookSession OpenOrCreate(string path, string? firstSheetName = null, string? appId = null)
    {
        var target = ClosedXmlLog.RequireNotBlank(path, nameof(path), ClosedXmlEvents.SessionRejected, ClosedXmlCatalog.Subcategories.Session);
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
        ArgumentNullException.ThrowIfNull(book);
        ArgumentNullException.ThrowIfNull(series);
        ClosedXmlLog.Debug(
            ClosedXmlEvents.SessionEnter,
            ClosedXmlCatalog.Subcategories.Session,
            "enter session",
            book.SessionId,
            ClosedXmlLog.Props(("via", "WriteSeries"), ("series", series.SeriesId), ("n", series.Count.ToString())),
            book.AppId);
        try
        {
            if (tableStyle is not null)
                book.TableStyle = tableStyle;
            SeriesWorkbook.Write(book, series, prefix, populationSize);
            ClosedXmlLog.Information(
                ClosedXmlEvents.WriteSeriesComplete,
                ClosedXmlCatalog.Subcategories.Session,
                "series workbook written",
                book.SessionId,
                ClosedXmlLog.Props(("series", series.SeriesId), ("n", series.Count.ToString()), ("sheets", book.SheetNames.Count.ToString())),
                book.AppId);
        }
        catch (Exception ex)
        {
            ClosedXmlLog.Unexpected(ClosedXmlEvents.SessionThrown, ClosedXmlCatalog.Subcategories.Session, ex, book.SessionId, book.AppId);
            throw;
        }
    }

    public static void Merge(WorkbookSession target, WorkbookSession source)
    {
        ArgumentNullException.ThrowIfNull(target);
        target.Merge(source);
    }
}
