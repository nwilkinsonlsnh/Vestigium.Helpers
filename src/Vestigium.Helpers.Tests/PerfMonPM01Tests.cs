using Vestigium.Helpers.PerfMon;

namespace Vestigium.Helpers.Tests;

public sealed class PerfMonPm01Tests
{
    [Fact]
    public void PM01_001_empty_category_is_rejected()
    {
        Assert.Throws<ArgumentException>(() => new CounterPath("", "% Processor Time"));
        Assert.Throws<ArgumentException>(() => new CounterPath("   ", "% Processor Time"));
    }

    [Fact]
    public void PM01_001_empty_counter_is_rejected()
    {
        Assert.Throws<ArgumentException>(() => new CounterPath("Processor", ""));
        Assert.Throws<ArgumentException>(() => new CounterPath("Processor", "   "));
    }

    [Fact]
    public void PM01_001_unavailable_value_is_null()
    {
        var path = new CounterPath("Processor", "% Processor Time", "_Total");
        var miss = SampleRecord.Unavailable(path);
        Assert.Equal(SampleStatus.Unavailable, miss.Status);
        Assert.Null(miss.Value);

        Assert.Throws<ArgumentException>(() => new SampleRecord(
            DateTimeOffset.UtcNow,
            "box",
            path.Category,
            path.Counter,
            path.Instance,
            value: 0d,
            path.Unit,
            SampleStatus.Unavailable));
    }

    [Fact]
    public void PM01_002_list_instances_respects_cap()
    {
        var fake = new FakeCounterSource();
        fake.SeedInstances("GPU Engine", Enumerable.Range(0, 10).Select(i => $"eng{i}"));

        var three = fake.ListInstances("GPU Engine", 3);
        Assert.Equal(["eng0", "eng1", "eng2"], three);
        Assert.Equal(3, three.Count);

        var all = fake.ListInstances("GPU Engine", 256);
        Assert.Equal(10, all.Count);

        Assert.Empty(fake.ListInstances("GPU Engine", 0));
        Assert.Empty(fake.ListInstances("GPU Engine", -1));
        Assert.Empty(fake.ListInstances("Missing", 256));
    }

    [Fact]
    public void PM01_002_read_returns_seeded_row_or_miss()
    {
        var path = new CounterPath("Processor", "% Processor Time", "_Total", "%");
        var other = new CounterPath("Processor", "% Processor Time", "0", "%");
        var fake = new FakeCounterSource();
        fake.Seed(path, SampleRecord.Ok(path, 12.5));
        fake.SeedMiss(other);

        var hit = fake.Read(path);
        Assert.Equal(SampleStatus.Ok, hit.Status);
        Assert.Equal(12.5, hit.Value);

        var miss = fake.Read(other);
        Assert.Equal(SampleStatus.Unavailable, miss.Status);
        Assert.Null(miss.Value);

        var unseeded = fake.Read(new CounterPath("Processor", "% User Time", "_Total"));
        Assert.Equal(SampleStatus.Unavailable, unseeded.Status);
        Assert.Null(unseeded.Value);
    }
}
