namespace Vestigium.Helpers.WinReg;

public sealed class RegistryCompareProgress
{
    public string Phase { get; init; } = "";
    public int KeysSeen { get; init; }
    public int ValuesSeen { get; init; }
    public string? CurrentPath { get; init; }
    public int LeftOnly { get; init; }
    public int RightOnly { get; init; }
    public int Changed { get; init; }
    public int Same { get; init; }
    public TimeSpan Elapsed { get; init; }
}

public sealed class RegistryDelta
{
    public required string Kind { get; init; }
    public required string Path { get; init; }
    public required string Name { get; init; }
    public string? LeftType { get; init; }
    public string? RightType { get; init; }
    public string? LeftHash { get; init; }
    public string? RightHash { get; init; }
    public string? LeftText { get; init; }
    public string? RightText { get; init; }
}

public sealed class RegistryCompareSummary
{
    public const int MaxDeltas = 256;

    public RegistryWriteStatus Status { get; init; }
    public string? Reason { get; init; }
    public string Verdict { get; init; } = "";
    public double Relatedness { get; init; }
    public double Delta { get; init; }
    public bool Stopped { get; init; }
    public int Same { get; init; }
    public int Changed { get; init; }
    public int LeftOnly { get; init; }
    public int RightOnly { get; init; }
    public string OutputPath { get; init; } = "";
    public IReadOnlyList<RegistryDelta> Deltas { get; init; } = [];
}
