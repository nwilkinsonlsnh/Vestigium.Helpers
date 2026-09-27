namespace Vestigium.Helpers.PerfMon.Cpu;

/// <summary>Live inventory. Tests inject a fake. Hosts use the PDH default.</summary>
public interface ICpuInventory
{
    bool CategoryPresent(string category);

    bool InstancePresent(string category, string instance);

    IReadOnlyList<string> LiveCounters(string category, string instance, int cap);

    IReadOnlyList<string> LiveInstances(string category, int cap);
}
