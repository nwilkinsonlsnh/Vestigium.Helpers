namespace Vestigium.Helpers.SystemInfo;

/// <summary>Processor topology. Cache sizes are bytes. No display strings.</summary>
public readonly record struct CpuTopology(
    int Sockets,
    int Cores,
    int Logical,
    long L1Bytes,
    long L2Bytes,
    long L3Bytes,
    long L4Bytes);

/// <summary>Census and clocks. Speeds are MHz. No display strings.</summary>
public readonly record struct CpuLive(
    int Processes,
    int Threads,
    int Handles,
    uint CurrentMhz,
    uint MaxMhz);
