using Vestigium.Helpers.Analytics;
using Vestigium.Helpers.Network;

namespace Vestigium.Helpers.Tests;

public sealed class NetworkPhase12Tests
{
    [Fact]
    public void P95_uses_analytics_and_labels_windows()
    {
        var samples = Enumerable.Repeat(20_000_000m, 20);
        var bill = NetworkHelper.BillP95(samples);
        Assert.Equal(0.95, bill.Percentile);
        Assert.Equal(20, bill.SampleCount);
        Assert.Equal(20_000_000m, bill.Rate.Bits);
        Assert.Equal(86_400, bill.Day.Seconds);
        Assert.Equal(2_592_000, bill.Days30.Seconds);
        Assert.Equal(31_536_000, bill.Year365.Seconds);
        Assert.False(string.IsNullOrWhiteSpace(bill.SeriesId));
    }

    [Fact]
    public void P95_from_numeric_series()
    {
        var series = NumericSeries.FromDecimal(Enumerable.Repeat(10_000_000m, 10), "pipe");
        var bill = NetworkHelper.BillP95(series);
        Assert.Equal(10_000_000m, bill.Rate.Bits);
        Assert.Equal(series.SeriesId, bill.SeriesId);
    }

    [Fact]
    public void P95_empty_throws()
        => Assert.Throws<ArgumentException>(() => NetworkHelper.BillP95(Array.Empty<decimal>()));

    [Fact]
    public void Oui_file_lookup_does_not_use_http()
    {
        var path = Path.Combine(Path.GetTempPath(), "vestigium-oui-" + Guid.NewGuid().ToString("N") + ".txt");
        File.WriteAllText(path, "# snapshot\n00:1A:2B\tAcme Instruments\n{\"oui\":\"00-50-56\",\"vendor\":\"VMware\"}\n");
        try
        {
            var acme = NetworkHelper.LookupOuiFile("00:1A:2B:3C:4D:5E", path);
            Assert.Equal("Acme Instruments", acme.Vendor);
            Assert.Equal(OuiSource.File, acme.Source);
            Assert.Contains("file snapshot", acme.Disclaimer, StringComparison.OrdinalIgnoreCase);

            var vm = NetworkHelper.LookupOuiFile("005056AABBCC", path);
            Assert.Equal("VMware", vm.Vendor);

            var miss = NetworkHelper.LookupOuiFile("FF:FF:FF:00:00:00", path);
            Assert.Null(miss.Vendor);
            Assert.Equal(OuiSource.None, miss.Source);
        }
        finally
        {
            File.Delete(path);
        }
    }
}
