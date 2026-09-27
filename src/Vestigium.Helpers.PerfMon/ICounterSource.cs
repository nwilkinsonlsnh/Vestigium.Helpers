namespace Vestigium.Helpers.PerfMon;

/// <summary>One read. Implementations must not invent a zero for a missing instance.</summary>
public interface ICounterSource
{
    SampleRecord Read(CounterPath path);

    IReadOnlyList<string> ListInstances(string category, int cap);

    /// <summary>
    /// Rate counters need a discarded first read. Level counters return false.
    /// </summary>
    bool NeedsPrime(CounterPath path) => false;
}
