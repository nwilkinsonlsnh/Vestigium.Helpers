using Vestigium.Helpers;
using Vestigium.Helpers.Analytics;
using Vestigium.Logging;

namespace Vestigium.Helpers.Csv;

/// <summary>
/// RFC 4180 CSV / TSV. Every public call writes through <see cref="HelperLog"/>,
/// which is the only door into Vestigium.Logging. This library never calls
/// <see cref="VestigiumLogger.Initialize"/>. Writes are silent until a host starts logging.
/// JSONL folder: <c>%ProgramData%\Vestigium\Logs\{APPID}</c>. Exports go to
/// <c>%DESKTOP%\Vestigium\Exports\{APPID}</c>.
/// </summary>
public static class CsvHelper
{
    public static string Identity => "Vestigium.Helpers.Csv";

    public static string Probe()
    {
        var app = HelperLog.AppIds.Csv;
        using var scope = HelperLog.Begin(app, HelperLog.Subcategories.Probe, "Probe");
        HelperLog.Information(app, VestigiumStatus.Pending, app, "Opening a CSV helper probe.");
        using var file = Create(app, new CsvOptions { Utf8Bom = false });
        file.WriteTable(CsvTable.Create(["Metric", "Value"], [["Identity", Identity]]));
        using var buffer = new MemoryStream();
        file.WriteTo(buffer);
        HelperLog.Information(app, VestigiumStatus.Success, app, "Csv probe complete. Identity=" + Identity);
        HelperLog.Exit(app, HelperLog.Subcategories.Probe, "Probe", $"bytes={buffer.Length}");
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
            ? $"vestigium-{id}-{stamp}.csv"
            : $"{stem.Trim()}-{stamp}.csv";
        foreach (var ch in Path.GetInvalidFileNameChars())
            file = file.Replace(ch, '_');
        return Path.Combine(DefaultExportDirectory(id), file);
    }

    public static CsvSession Create(string? appId = null, CsvOptions? options = null)
    {
        var app = string.IsNullOrWhiteSpace(appId) ? HelperLog.AppIds.Csv : appId.Trim();
        var sessionId = HelperLog.NewId();
        var dialect = CsvOptions.Resolve(options);
        using var scope = HelperLog.Begin(app, HelperLog.Subcategories.Session, "Create", $"session={sessionId}", sessionId);
        try
        {
            HelperLog.Information(app, VestigiumStatus.Pending, HelperLog.Subcategories.Session, $"Creating a blank CSV session={sessionId}");
            return new CsvSession(table: null, path: null, app, sessionId, dialect);
        }
        catch (Exception ex)
        {
            HelperLog.Trap(ex);
            throw;
        }
    }

    public static CsvSession Open(string path, string? appId = null, CsvOptions? options = null)
    {
        var app = string.IsNullOrWhiteSpace(appId) ? HelperLog.AppIds.Csv : appId.Trim();
        var sessionId = HelperLog.NewId();
        var dialect = CsvOptions.Resolve(options);
        using var scope = HelperLog.Begin(app, HelperLog.Subcategories.Session, "Open", $"path={path} session={sessionId}", sessionId);
        var target = HelperGuard.FileExists(path, nameof(path));
        HelperLog.Information(app, VestigiumStatus.Pending, HelperLog.Subcategories.Session, $"Opening CSV path={target} session={sessionId}");
        try
        {
            var bytes = File.ReadAllBytes(target);
            var table = CsvCodec.ReadBytes(bytes, dialect);
            return new CsvSession(table, target, app, sessionId, dialect);
        }
        catch (Exception ex)
        {
            HelperLog.Trap(ex);
            throw;
        }
    }

    public static CsvSession OpenOrCreate(string path, string? appId = null, CsvOptions? options = null)
    {
        var target = HelperGuard.NotBlank(path, nameof(path));
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
        HelperGuard.NotNull(stream, nameof(stream));
        using var buffer = new MemoryStream();
        stream.CopyTo(buffer);
        var dialect = CsvOptions.Resolve(options);
        var app = string.IsNullOrWhiteSpace(appId) ? HelperLog.AppIds.Csv : appId.Trim();
        using var scope = HelperLog.Begin(app, HelperLog.Subcategories.Session, "Read", "(stream)");
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
