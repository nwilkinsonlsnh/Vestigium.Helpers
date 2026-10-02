namespace Vestigium.Helpers.SystemInfo;

/// <summary>Outcome of a machine-fact read. Unavailable is not a zero measurement.</summary>
public enum FactStatus
{
    Ok = 0,
    Unavailable = 1
}

/// <summary>One snapshot. A missing read has no value.</summary>
public readonly record struct Fact<T>
{
    public FactStatus Status { get; init; }

    public T? Value { get; init; }

    public bool IsOk => Status == FactStatus.Ok && Value is not null;

    public static Fact<T> Ok(T value)
        => new() { Status = FactStatus.Ok, Value = value };

    public static Fact<T> Unavailable()
        => new() { Status = FactStatus.Unavailable };
}
