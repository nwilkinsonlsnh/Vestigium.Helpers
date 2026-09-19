using Vestigium.Helpers.Analytics;

namespace Vestigium.Helpers.Csv;

internal static class SeriesCsv
{
    public static CsvTable Sample(NumericSeries series)
    {
        ArgumentNullException.ThrowIfNull(series);
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
        ArgumentNullException.ThrowIfNull(file);
        ArgumentNullException.ThrowIfNull(series);
        try
        {
            file.WriteTable(Sample(series));
            CsvLog.Information(
                CsvEvents.WriteSeriesComplete,
                CsvCatalog.Subcategories.Series,
                "series csv written",
                file.SessionId,
                CsvLog.Props(("series", series.SeriesId), ("n", series.Count.ToString())),
                file.AppId);
        }
        catch (Exception ex)
        {
            CsvLog.Unexpected(CsvEvents.SessionThrown, CsvCatalog.Subcategories.Series, ex, file.SessionId, file.AppId);
            throw;
        }
    }
}
