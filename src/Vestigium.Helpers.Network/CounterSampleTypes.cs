namespace Vestigium.Helpers.Network;

public sealed class CounterSampleOptions
{
    public static readonly TimeSpan MinDuration = TimeSpan.FromMilliseconds(10);
    public static readonly TimeSpan MaxDuration = TimeSpan.FromHours(1);

    public TimeSpan Duration { get; set; } = TimeSpan.FromSeconds(1);
    public TimeSpan? Interval { get; set; }
}

public sealed record CounterReading(
    long BytesIn,
    long BytesOut,
    long ErrorsIn,
    long ErrorsOut,
    long DiscardsIn,
    long DiscardsOut);

public sealed record CounterSample(
    DateTimeOffset CapturedUtc,
    CounterReading Reading);

public sealed record CounterSampleResult(
    string JobId,
    string AdapterName,
    string AdapterId,
    TimeSpan Duration,
    CounterReading Start,
    CounterReading End,
    CounterReading Delta,
    IReadOnlyList<CounterSample> Samples);
