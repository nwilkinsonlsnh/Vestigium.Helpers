using System.Security.Cryptography;
using Vestigium.Helpers;
using Vestigium.Helpers.Analytics;
using Vestigium.Helpers.ClosedXml;
using Vestigium.Logging;

return HelperDemoHost.Run(
    HelperLog.AppIds.ClosedXml,
    WorkbookHelper.Identity,
    ClosedXmlDemo.Run);

internal static class ClosedXmlDemo
{
    private const int SampleSize = 1000;
    private const int PopulationSize = 100_000;

    public static void Run()
    {
        var app = HelperLog.AppIds.ClosedXml;
        WorkbookHelper.Probe();

        HelperLog.Information(app, VestigiumStatus.Pending, app,
            $"Drawing {SampleSize} unique integers from 1..{PopulationSize} for an Excel dump.");

        var origin = new DateTimeOffset(2026, 9, 7, 16, 0, 0, TimeSpan.Zero);
        var observations = DrawUnique(SampleSize, PopulationSize, origin);
        var series = NumericSeries.FromObservations(observations, "crypto-1k");

        using var book = WorkbookHelper.Create("Summary", app);
        WorkbookHelper.WriteSeries(book, series, populationSize: PopulationSize);
        var path = book.Save();

        Console.WriteLine("Workbook written:");
        Console.WriteLine($"  {path}");
        Console.WriteLine($"  sheets : {string.Join(", ", book.SheetNames)}");
        Console.WriteLine($"  style  : {ExcelTableStyles.ToExcelName(book.TableStyle)}");
        Console.WriteLine($"  n      : {series.Count}");
        Console.WriteLine($"  mean   : {series.Full.Mean}");
        Console.WriteLine($"  P50    : {series.Full.Median}");
        Console.WriteLine($"  P95    : {series.Full.Percentile(0.95)}  (percentile, not confidence)");
        Console.WriteLine($"  export : {WorkbookHelper.DefaultExportDirectory(app)}");
        Console.WriteLine();
        Console.WriteLine("Logs stay under %ProgramData%\\Vestigium\\Logs\\ClosedXml\\");
        Console.WriteLine("Workbooks go to %DESKTOP%\\Vestigium\\Exports\\ClosedXml\\");

        HelperLog.Information(
            app,
            VestigiumStatus.Success,
            app,
            $"Wrote {path} n={series.Count} mean={series.Full.Mean:F2} P95={series.Full.Percentile(0.95)} sheets={book.SheetNames.Count}");
    }

    private static List<Observation> DrawUnique(int count, int populationSize, DateTimeOffset origin)
    {
        var drawn = new HashSet<int>(count);
        while (drawn.Count < count)
            drawn.Add(RandomNumberGenerator.GetInt32(1, populationSize + 1));

        var values = drawn.ToArray();
        var list = new List<Observation>(count);
        for (var i = 0; i < values.Length; i++)
            list.Add(new Observation(values[i], origin.AddSeconds(i)));
        return list;
    }
}
