using Vestigium.Helpers;
using Vestigium.Helpers.Analytics;

return HelperDemoHost.Run(
    HelperLog.AppIds.Analytics,
    AnalyticsHelper.Identity,
    static () =>
    {
        AnalyticsHelper.Probe();
        var series = NumericSeries.From(
            new[] { 12.4, 11.9, 13.1, 12.0, 18.7, 12.2, 12.5, 11.8, 40.2, 12.1 },
            "rtt-ms");
        var full = series.Full;
        var ci = series.Confidence(0.95);

        Console.WriteLine($"n           {series.Count}");
        Console.WriteLine($"five-number {full.Min} / {full.Q1} / {full.Median} / {full.Q3} / {full.Max}");
        Console.WriteLine($"P90         {full.Percentile(0.90)}");
        Console.WriteLine($"P95         {full.Percentile(0.95)}  (percentile, not confidence)");
        Console.WriteLine($"skew        {full.Skewness}");
        Console.WriteLine($"Q4 count    {series.Q4.Count}  (right tail)");
        Console.WriteLine($"high out    {string.Join(", ", full.HighOutliers)}");
        Console.WriteLine($"mean 95% CI {ci.Mean.Lower:F2} … {ci.Mean.Upper:F2}");
    });
