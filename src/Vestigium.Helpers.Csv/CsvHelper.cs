using Vestigium.Helpers.Analytics;
using Vestigium.Logging;

namespace Vestigium.Helpers.Csv;

/// <summary>
/// RFC 4180 CSV / TSV. Logging goes through Vestigium.Logging (APPID Csv, EVENTID 11500+).
/// This library never calls <see cref="VestigiumLogger.Initialize"/>.
/// </summary>
public static class CsvHelper
{
    public static string Identity => "Vestigium.Helpers.Csv";

    public static string Probe()
    {
        CsvLog.Debug(CsvEvents.ProbeEnter, CsvCatalog.Subcategories.Probe, "enter Probe");
        using var file = Create(CsvCatalog.AppId, new CsvOptions { Utf8Bom = false });
        file.WriteTable(CsvTable.Create(["Metric", "Value"], [["Identity", Identity]]));
        using var buffer = new MemoryStream();
        file.WriteTo(buffer);
        CsvLog.Information(
            CsvEvents.ProbeComplete,
            CsvCatalog.Subcategories.Probe,
            "probe complete",
            file.SessionId,
            CsvLog.Props(("identity", Identity), ("bytes", buffer.Length.ToString())));
        return Identity;
    }

    public static string DefaultExportDirectory(string appId)
    {
        var id = CsvLog.RequireNotBlank(appId, nameof(appId));
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
        var id = CsvLog.RequireNotBlank(appId, nameof(appId));
        var stamp = DateTime.Now.ToString("yyyyMMdd-HHmmss");
        var file = string.IsNullOrWhiteSpace(stem)
            ? $"vestigium-{id}-{stamp}.csv"
            : $"{stem.Trim()}-{stamp}.csv";
        foreach (var ch in Path.GetInvalidFileNameChars())
            file = file.Replace(ch, '_');
        return Path.Combine(DefaultExportDirectory(id), file);
    }

    public static CsvSession Create(string? appId = null, CsvOptions? options = null)
    {
        var app = string.IsNullOrWhiteSpace(appId) ? CsvCatalog.AppId : appId.Trim();
        var sessionId = CsvLog.NewId();
        var dialect = CsvOptions.Resolve(options);
        CsvLog.Debug(CsvEvents.SessionEnter, CsvCatalog.Subcategories.Session, "enter session",
            sessionId, CsvLog.Props(("via", "Create")), app);
        try
        {
            return new CsvSession(table: null, path: null, app, sessionId, dialect);
        }
        catch (Exception ex)
        {
            CsvLog.Unexpected(CsvEvents.SessionThrown, CsvCatalog.Subcategories.Session, ex, sessionId, app);
            throw;
        }
    }

    public static CsvSession Open(string path, string? appId = null, CsvOptions? options = null)
    {
        var app = string.IsNullOrWhiteSpace(appId) ? CsvCatalog.AppId : appId.Trim();
        var sessionId = CsvLog.NewId();
        var dialect = CsvOptions.Resolve(options);
        var target = CsvLog.RequireFile(path, nameof(path));
        CsvLog.Debug(CsvEvents.SessionEnter, CsvCatalog.Subcategories.Session, "enter session",
            sessionId, CsvLog.Props(("via", "Open"), ("path", target)), app);
        try
        {
            var bytes = File.ReadAllBytes(target);
            var table = CsvCodec.ReadBytes(bytes, dialect);
            var session = new CsvSession(table, target, app, sessionId, dialect);
            CsvLog.Information(CsvEvents.SessionOpened, CsvCatalog.Subcategories.Session, "session opened",
                sessionId, CsvLog.Props(("path", target), ("rows", table.Rows.Count.ToString()), ("cols", table.Headers.Count.ToString())), app);
            return session;
        }
        catch (Exception ex)
        {
            CsvLog.Unexpected(CsvEvents.SessionThrown, CsvCatalog.Subcategories.Session, ex, sessionId, app);
            throw;
        }
    }

    public static CsvSession OpenOrCreate(string path, string? appId = null, CsvOptions? options = null)
    {
        var target = CsvLog.RequireNotBlank(path, nameof(path));
        if (File.Exists(target))
            return Open(target, appId, options);

        var session = Create(appId, options);
        session.SaveAs(target);
        return session;
    }

    public static string WriteTable(CsvTable table, string path, CsvOptions? options = null, string? appId = null)
    {
        using var file = Create(appId, options);
        file.WriteTable(table);
        return file.SaveAs(path);
    }

    public static void WriteTable(CsvTable table, Stream stream, CsvOptions? options = null, string? appId = null)
    {
        using var file = Create(appId, options);
        file.WriteTable(table);
        file.WriteTo(stream);
    }

    public static CsvTable Read(string path, CsvOptions? options = null, string? appId = null)
    {
        using var file = Open(path, appId, options);
        return file.Read();
    }

    public static CsvTable Read(Stream stream, CsvOptions? options = null, string? appId = null)
    {
        ArgumentNullException.ThrowIfNull(stream);
        using var buffer = new MemoryStream();
        stream.CopyTo(buffer);
        var dialect = CsvOptions.Resolve(options);
        var app = string.IsNullOrWhiteSpace(appId) ? CsvCatalog.AppId : appId.Trim();
        CsvLog.Debug(CsvEvents.SessionEnter, CsvCatalog.Subcategories.Session, "enter session",
            properties: CsvLog.Props(("via", "ReadStream")), appId: app);
        return CsvCodec.ReadBytes(buffer.ToArray(), dialect);
    }

    public static void WriteSeries(CsvSession file, NumericSeries series)
        => SeriesCsv.Write(file, series);

    public static string WriteSeries(NumericSeries series, string path, CsvOptions? options = null, string? appId = null)
    {
        using var file = Create(appId, options);
        WriteSeries(file, series);
        return file.SaveAs(path);
    }
}
