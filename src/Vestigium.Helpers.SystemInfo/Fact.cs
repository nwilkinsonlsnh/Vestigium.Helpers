namespace Vestigium.Helpers.SystemInfo;

/// <summary>Outcome of a machine-fact read. Unavailable is not a zero measurement.</summary>
public enum FactStatus
{
    Ok = 0,
    Unavailable = 1
}

/// <summary>One snapshot. A missing read has no readable value, including for value types.</summary>
public readonly record struct Fact<T>
{
    private readonly T _value;

    public FactStatus Status { get; init; }

    public bool IsOk => Status == FactStatus.Ok;

    public T Value => IsOk ? _value : throw new InvalidOperationException("Fact is unavailable.");

    public static Fact<T> Ok(T value)
        => new() { Status = FactStatus.Ok, _value = value };

    public static Fact<T> Unavailable()
        => new() { Status = FactStatus.Unavailable };
}
