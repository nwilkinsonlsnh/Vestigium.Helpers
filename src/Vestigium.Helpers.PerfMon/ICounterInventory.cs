namespace Vestigium.Helpers.PerfMon;

/// <summary>Live PDH inventory. Tests inject a fake. Probes do not each invent one.</summary>
public interface ICounterInventory
{
    bool CategoryPresent(string category);

    bool InstancePresent(string category, string instance);

    IReadOnlyList<string> LiveCounters(string category, string instance, int cap);

    IReadOnlyList<string> LiveInstances(string category, int cap);
}
