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
