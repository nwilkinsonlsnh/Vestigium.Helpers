using System.Diagnostics;
using Vestigium.Helpers.PerfMon;

namespace Vestigium.Helpers.Tests;

[Collection("Logger")]
public sealed class PerfMonPr03cSourceTests
{
    [Fact]
    public void PR03c_006_missing_instance_is_unavailable()
    {
        var path = new CounterPath("Processor", "% Processor Time", "VestigiumNoSuchInstance", "%");
        using var source = new CachedPdhSource();
        var row = source.Read(path);
        Assert.Equal(SampleStatus.Unavailable, row.Status);
        Assert.Null(row.Value);
        Assert.Equal(0, source.OpenedCount);
    }

    [Fact]
    public void PR03c_006_dispose_drops_opened_counters()
    {
        var category = LiveProcessorCategory();
        if (category is null)
            return;

        var path = new CounterPath(category, "% Processor Time", "_Total", "%");
        var source = new CachedPdhSource();
        _ = source.Read(path);
        Assert.Equal(1, source.OpenedCount);

        source.Dispose();
        Assert.Equal(0, source.OpenedCount);
        source.Dispose();
        Assert.Equal(0, source.OpenedCount);
    }

    [Fact]
    public void PR03c_006_retain_drops_the_other_instance()
    {
        var category = LiveProcessorCategory();
        if (category is null)
            return;

        var total = new CounterPath(category, "% Processor Time", "_Total", "%");
        var zero = new CounterPath(category, "% Processor Time", "0", "%");
        using var source = new CachedPdhSource();
        _ = source.Read(total);
        _ = source.Read(zero);
        if (source.OpenedCount < 2)
            return;

        source.Retain([total]);
        Assert.Equal(1, source.OpenedCount);
    }

    [Fact]
    public void PR03c_006_first_rate_read_is_unavailable()
    {
        var category = LiveProcessorCategory();
        if (category is null)
            return;

        var path = new CounterPath(category, "% Processor Time", "_Total", "%");
        using var source = new CachedPdhSource();
        Assert.True(source.NeedsPrime(path));

        var prime = source.Read(path);
        Assert.Equal(SampleStatus.Unavailable, prime.Status);
        Assert.Null(prime.Value);

        var row = source.Read(path);
        Assert.Equal(SampleStatus.Ok, row.Status);
        Assert.NotNull(row.Value);
        Assert.False(source.NeedsPrime(path));
    }

    [Fact]
    public async Task PR03c_006_job_does_not_emit_the_prime()
    {
        var category = LiveProcessorCategory();
        if (category is null)
            return;

        var path = new CounterPath(category, "% Processor Time", "_Total", "%");
        var job = new SampleJob([path], new SampleJobOptions { Count = 1 });
        var result = await job.RunAsync();
        Assert.Equal(SampleStatus.Ok, result.Status);
        Assert.Single(result.Samples);
        Assert.Equal(SampleStatus.Ok, result.Samples[0].Status);
        Assert.NotNull(result.Samples[0].Value);
    }

    [Fact]
    public void PR03c_006_nic_rate_is_a_rate_after_prime()
    {
        var instance = LiveNic();
        if (instance is null)
            return;

        var path = new CounterPath("Network Interface", "Bytes Received/sec", instance, "/sec");
        using var source = new CachedPdhSource();
        var prime = source.Read(path);
        Assert.Equal(SampleStatus.Unavailable, prime.Status);
        Assert.Null(prime.Value);

        var row = source.Read(path);
        Assert.Equal(SampleStatus.Ok, row.Status);
        Assert.NotNull(row.Value);
        Assert.True(row.Value >= 0);
    }

    private static string? LiveProcessorCategory()
    {
        try
        {
            if (PerformanceCounterCategory.Exists("Processor"))
                return "Processor";
            if (PerformanceCounterCategory.Exists("Processor Information"))
                return "Processor Information";
        }
        catch (InvalidOperationException)
        {
            return null;
        }
        catch (ArgumentException)
        {
            return null;
        }

        return null;
    }

    private static string? LiveNic()
    {
        try
        {
            if (!PerformanceCounterCategory.Exists("Network Interface"))
                return null;
            using var source = new CachedPdhSource();
            var names = source.ListInstances("Network Interface", 32);
            return names.FirstOrDefault(n =>
                n.Length > 0
                && !n.Equals("_Total", StringComparison.OrdinalIgnoreCase)
                && !n.Contains("Loopback", StringComparison.OrdinalIgnoreCase));
        }
        catch (InvalidOperationException)
        {
            return null;
        }
        catch (ArgumentException)
        {
            return null;
        }
    }
}
