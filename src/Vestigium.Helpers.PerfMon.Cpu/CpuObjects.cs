namespace Vestigium.Helpers.PerfMon.Cpu;

/// <summary>PDH objects this probe is allowed to name.</summary>
public static class CpuObjects
{
    public const string Processor = "Processor";
    public const string ProcessorInformation = "Processor Information";
    public const string ProcessorPerformance = "Processor Performance";

    public static IReadOnlyList<string> All { get; } =
    [
        Processor,
        ProcessorInformation,
        ProcessorPerformance
    ];
}
