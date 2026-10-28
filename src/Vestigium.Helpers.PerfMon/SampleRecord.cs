namespace Vestigium.Helpers.PerfMon;

/// <summary>One sample. Frozen. Unavailable carries a null value, never a stand-in zero.</summary>
public sealed record SampleRecord
{
    public SampleRecord(
        DateTimeOffset utc,
        string machine,
        string category,
        string counter,
        string instance,
        double? value,
        string unit,
        SampleStatus status)
    {
        if (status == SampleStatus.Unavailable && value is not null)
            throw new ArgumentException("Unavailable sample cannot carry a value.", nameof(value));

        Utc = utc;
        Machine = string.IsNullOrWhiteSpace(machine) ? Environment.MachineName : machine.Trim();
        Category = category?.Trim() ?? string.Empty;
        Counter = counter?.Trim() ?? string.Empty;
        Instance = instance?.Trim() ?? string.Empty;
        Value = value;
        Unit = unit?.Trim() ?? string.Empty;
        Status = status;
    }

    public DateTimeOffset Utc { get; }
    public string Machine { get; }
    public string Category { get; }
    public string Counter { get; }
    public string Instance { get; }
    public double? Value { get; }
    public string Unit { get; }
    public SampleStatus Status { get; }

    public static SampleRecord Ok(CounterPath path, double value, DateTimeOffset? utc = null, string? machine = null)
    {
        ArgumentNullException.ThrowIfNull(path);
        return new SampleRecord(
            utc ?? DateTimeOffset.UtcNow,
            machine ?? Environment.MachineName,
            path.Category,
            path.Counter,
            path.Instance,
            value,
            path.Unit,
            SampleStatus.Ok);
    }

    public static SampleRecord Unavailable(CounterPath path, DateTimeOffset? utc = null, string? machine = null)
    {
        ArgumentNullException.ThrowIfNull(path);
        return new SampleRecord(
            utc ?? DateTimeOffset.UtcNow,
            machine ?? Environment.MachineName,
            path.Category,
            path.Counter,
            path.Instance,
            value: null,
            path.Unit,
            SampleStatus.Unavailable);
    }
}
