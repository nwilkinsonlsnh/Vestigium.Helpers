using Vestigium.Helpers;
using Vestigium.Helpers.Analytics;
using Vestigium.Logging;

namespace Vestigium.Helpers.Csv;

internal static class SeriesCsv
{
    public static CsvTable Sample(NumericSeries series)
    {
        HelperGuard.NotNull(series, nameof(series));
        var rows = new List<IReadOnlyList<object?>>(series.Count);
        for (var i = 0; i < series.Count; i++)
        {
            object? timestamp = null;
            if (series.Times.Count == series.Count && series.Times[i] is { } at)
                timestamp = at;
            rows.Add([i, series.Values[i], timestamp]);
        }

        return CsvTable.Create(["Index", "Value", "Timestamp"], rows, "Sample");
    }

    public static void Write(CsvSession file, NumericSeries series)
    {
        HelperGuard.NotNull(file, nameof(file));
        HelperGuard.NotNull(series, nameof(series));
        using var scope = file.Trace(
            "WriteSeries",
            $"series={series.SeriesId} n={series.Count} name={series.Name ?? "(none)"}");
        try
        {
            file.WriteTable(Sample(series));
            HelperLog.Information(
                file.AppId,
                VestigiumStatus.Success,
                HelperLog.Subcategories.Series,
                $"WriteSeries series={series.SeriesId} n={series.Count} session={file.SessionId}");
        }
        catch (Exception ex)
        {
            HelperLog.Trap(ex);
            throw;
        }
    }
}
