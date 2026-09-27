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

    [Fact]
    public void PM01_003_interval_below_200ms_rejected()
    {
        var paths = OnePath();
        Assert.Throws<ArgumentOutOfRangeException>(() => new SampleJob(paths, new SampleJobOptions
        {
            Interval = TimeSpan.FromMilliseconds(199),
            Count = 1
        }));
    }

    [Fact]
    public void PM01_003_burst_below_50ms_rejected()
    {
        var paths = OnePath();
        Assert.Throws<ArgumentOutOfRangeException>(() => new SampleJob(paths, new SampleJobOptions
        {
            Interval = TimeSpan.FromMilliseconds(49),
            AllowBurst = true,
            Count = 1
        }));
    }

    [Fact]
    public void PM01_003_duration_over_24h_rejected()
    {
        var paths = OnePath();
        Assert.Throws<ArgumentOutOfRangeException>(() => new SampleJob(paths, new SampleJobOptions
        {
            Duration = TimeSpan.FromHours(24).Add(TimeSpan.FromMilliseconds(1))
        }));
    }

    [Fact]
    public void PM01_003_unbounded_job_rejected()
    {
        var job = new SampleJob(OnePath(), new SampleJobOptions { Count = 0 });
        Assert.Throws<ArgumentException>(() => job.RunAsync().GetAwaiter().GetResult());

        var missing = new SampleJob(OnePath(), new SampleJobOptions());
        Assert.Throws<ArgumentException>(() => missing.RunAsync().GetAwaiter().GetResult());
    }

    [Fact]
    public void PM01_003_empty_path_list_rejected()
    {
        Assert.Throws<ArgumentException>(() => new SampleJob([]));
        Assert.Throws<ArgumentNullException>(() => new SampleJob(null!));
    }

    [Fact]
    public async Task PM01_004_count_stops_the_job()
    {
        var path = new CounterPath("Processor", "% Processor Time", "_Total");
        var fake = new FakeCounterSource();
        fake.Seed(path, SampleRecord.Ok(path, 1), SampleRecord.Ok(path, 2), SampleRecord.Ok(path, 3));

        var job = new SampleJob([path], new SampleJobOptions
        {
            Count = 2,
            Source = fake,
            Clock = new ImmediateClock()
        });

        var result = await job.RunAsync();
        Assert.Equal(SampleStatus.Ok, result.Status);
        Assert.Equal(2, result.Samples.Count);
        Assert.Equal(1, result.Samples[0].Value);
        Assert.Equal(2, result.Samples[1].Value);
        Assert.True(((IList<SampleRecord>)result.Samples).IsReadOnly
            || result.Samples is SampleRecord[]);
    }

    [Fact]
    public async Task PM01_004_duration_stops_the_job()
    {
        var path = new CounterPath("Processor", "% Processor Time", "_Total");
        var fake = new FakeCounterSource();
        fake.Seed(path,
            SampleRecord.Ok(path, 1),
            SampleRecord.Ok(path, 2),
            SampleRecord.Ok(path, 3),
            SampleRecord.Ok(path, 4));

        var job = new SampleJob([path], new SampleJobOptions
        {
            Duration = TimeSpan.FromSeconds(2),
            Interval = TimeSpan.FromSeconds(1),
            Source = fake,
            Clock = new ImmediateClock()
        });

        var result = await job.RunAsync();
        Assert.Equal(SampleStatus.Ok, result.Status);
        Assert.Equal(2, result.Samples.Count);
    }

    [Fact]
    public async Task PM01_004_cancel_keeps_samples()
    {
        var path = new CounterPath("Processor", "% Processor Time", "_Total");
        var fake = new FakeCounterSource();
        fake.Seed(path, SampleRecord.Ok(path, 1), SampleRecord.Ok(path, 2), SampleRecord.Ok(path, 3));
        var cts = new CancellationTokenSource();
        var source = new CancelAfterRead(fake, cts, afterReads: 1);

        var job = new SampleJob([path], new SampleJobOptions
        {
            Count = 10,
            Source = source,
            Clock = new ImmediateClock()
        });

        var result = await job.RunAsync(cts.Token);
        Assert.Equal(SampleStatus.Cancelled, result.Status);
        Assert.Equal(1, result.Samples.Count);
        Assert.Equal(1, result.Samples[0].Value);
    }

    [Fact]
    public async Task PM01_004_missing_instance_is_partial()
    {
        var okPath = new CounterPath("Processor", "% Processor Time", "_Total");
        var missPath = new CounterPath("Processor", "% Processor Time", "99");
        var fake = new FakeCounterSource();
        fake.Seed(okPath, SampleRecord.Ok(okPath, 40));
        fake.SeedMiss(missPath);

        var job = new SampleJob([okPath, missPath], new SampleJobOptions
        {
            Count = 1,
            Source = fake,
            Clock = new ImmediateClock()
        });

        var result = await job.RunAsync();
        Assert.Equal(SampleStatus.Partial, result.Status);
        Assert.Equal(2, result.Samples.Count);
        Assert.Equal(SampleStatus.Ok, result.Samples[0].Status);
        Assert.Equal(SampleStatus.Unavailable, result.Samples[1].Status);
        Assert.Null(result.Samples[1].Value);
    }

    private static CounterPath[] OnePath()
        => [new CounterPath("Processor", "% Processor Time", "_Total")];

    private sealed class ImmediateClock : TimeProvider
    {
        private DateTimeOffset _utc = new(2026, 9, 27, 19, 0, 0, TimeSpan.Zero);

        public override DateTimeOffset GetUtcNow() => _utc;

        public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period)
        {
            if (dueTime != Timeout.InfiniteTimeSpan)
            {
                if (dueTime > TimeSpan.Zero)
                    _utc += dueTime;
                callback(state);
            }

            return new DoneTimer();
        }

        private sealed class DoneTimer : ITimer
        {
            public bool Change(TimeSpan dueTime, TimeSpan period) => false;
            public void Dispose() { }
            public ValueTask DisposeAsync() => ValueTask.CompletedTask;
        }
    }

    private sealed class CancelAfterRead : ICounterSource
    {
        private readonly ICounterSource _inner;
        private readonly CancellationTokenSource _cts;
        private readonly int _afterReads;
        private int _reads;

        public CancelAfterRead(ICounterSource inner, CancellationTokenSource cts, int afterReads)
        {
            _inner = inner;
            _cts = cts;
            _afterReads = afterReads;
        }

        public SampleRecord Read(CounterPath path)
        {
            var row = _inner.Read(path);
            _reads++;
            if (_reads >= _afterReads)
                _cts.Cancel();
            return row;
        }

        public IReadOnlyList<string> ListInstances(string category, int cap)
            => _inner.ListInstances(category, cap);
    }
}

