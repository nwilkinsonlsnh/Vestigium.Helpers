using System.Diagnostics;

namespace Vestigium.Helpers.Processes;

internal readonly record struct ProcessGpuSample(double? UsagePercent, long? DedicatedBytes, long? SystemBytes);

internal sealed class SystemGpuSample
{
    public double? UsagePercent { get; init; }
    public long? DedicatedBytes { get; init; }
    public long? SystemBytes { get; init; }
    public IReadOnlyList<GpuAdapterInfo> Adapters { get; init; } = [];
}

internal static class ProcessGpuCatalog
{
    private static readonly object Gate = new();
    private static DateTimeOffset _next;
    private static Dictionary<int, ProcessGpuSample> _byPid = [];
    private static SystemGpuSample _system = new();
    private static bool _supported;

    internal static ProcessGpuSample ForPid(int pid)
    {
        Refresh();
        return _byPid.TryGetValue(pid, out var sample) ? sample : default;
    }

    internal static SystemGpuSample System()
    {
        Refresh();
        return _system;
    }

    internal static bool Supported
    {
        get { Refresh(); return _supported; }
    }

    private static void Refresh()
    {
        lock (Gate)
        {
            if (DateTimeOffset.UtcNow < _next)
                return;
            _next = DateTimeOffset.UtcNow.AddMilliseconds(750);
            try { Read(); }
            catch
            {
                _supported = false;
                _byPid = [];
                _system = new SystemGpuSample();
            }
        }
    }

    private static void Read()
    {
        var usage = new Dictionary<int, double>();
        var dedicated = new Dictionary<int, long>();
        var shared = new Dictionary<int, long>();
        var adapterUse = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
        var adapterDedicated = new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase);
        var adapterShared = new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase);
        var any = false;

        if (CategoryExists("GPU Engine"))
        {
            any = true;
            foreach (var row in ReadInstances("GPU Engine", "Utilization Percentage"))
            {
                if (TryPid(row.Name, out var pid))
                    usage[pid] = usage.GetValueOrDefault(pid) + row.Value;
                var adapter = AdapterKey(row.Name);
                adapterUse[adapter] = adapterUse.GetValueOrDefault(adapter) + row.Value;
            }
        }

        if (CategoryExists("GPU Process Memory"))
        {
            any = true;
            foreach (var row in ReadInstances("GPU Process Memory", "Dedicated Usage"))
            {
                if (TryPid(row.Name, out var pid))
                    dedicated[pid] = dedicated.GetValueOrDefault(pid) + (long)row.Value;
            }
            foreach (var row in ReadInstances("GPU Process Memory", "Shared Usage"))
            {
                if (TryPid(row.Name, out var pid))
                    shared[pid] = shared.GetValueOrDefault(pid) + (long)row.Value;
            }
        }

        if (CategoryExists("GPU Adapter Memory"))
        {
            any = true;
            foreach (var row in ReadInstances("GPU Adapter Memory", "Dedicated Usage"))
            {
                var adapter = string.IsNullOrWhiteSpace(row.Name) ? "adapter" : row.Name;
                adapterDedicated[adapter] = adapterDedicated.GetValueOrDefault(adapter) + (long)row.Value;
            }
            foreach (var row in ReadInstances("GPU Adapter Memory", "Shared Usage"))
            {
                var adapter = string.IsNullOrWhiteSpace(row.Name) ? "adapter" : row.Name;
                adapterShared[adapter] = adapterShared.GetValueOrDefault(adapter) + (long)row.Value;
            }
        }

        _supported = any;
        var map = new Dictionary<int, ProcessGpuSample>();
        foreach (var pid in usage.Keys.Concat(dedicated.Keys).Concat(shared.Keys).Distinct())
        {
            map[pid] = new ProcessGpuSample(
                usage.TryGetValue(pid, out var pct) ? Math.Min(100, pct) : null,
                dedicated.TryGetValue(pid, out var ded) ? ded : null,
                shared.TryGetValue(pid, out var sh) ? sh : null);
        }
        _byPid = map;

        var adapters = new List<GpuAdapterInfo>();
        foreach (var name in adapterUse.Keys.Concat(adapterDedicated.Keys).Concat(adapterShared.Keys).Distinct(StringComparer.OrdinalIgnoreCase))
        {
            adapters.Add(new GpuAdapterInfo
            {
                Name = name,
                UsagePercent = adapterUse.TryGetValue(name, out var pct) ? Math.Min(100, pct) : null,
                DedicatedBytes = adapterDedicated.TryGetValue(name, out var ded) ? ded : null,
                SystemBytes = adapterShared.TryGetValue(name, out var sh) ? sh : null
            });
        }

        _system = any
            ? new SystemGpuSample
            {
                UsagePercent = adapterUse.Count == 0 ? null : Math.Min(100, adapterUse.Values.DefaultIfEmpty(0).Max()),
                DedicatedBytes = adapterDedicated.Count == 0 ? dedicated.Values.DefaultIfEmpty(0).Sum() : adapterDedicated.Values.Sum(),
                SystemBytes = adapterShared.Count == 0 ? shared.Values.DefaultIfEmpty(0).Sum() : adapterShared.Values.Sum(),
                Adapters = adapters
            }
            : new SystemGpuSample();
        if (!any) _byPid = [];
    }

    private static bool CategoryExists(string name)
    {
        try { return PerformanceCounterCategory.Exists(name); }
        catch { return false; }
    }

    private static List<(string Name, double Value)> ReadInstances(string category, string counter)
    {
        var rows = new List<(string, double)>();
        string[] instances;
        try { instances = new PerformanceCounterCategory(category).GetInstanceNames(); }
        catch { return rows; }

        foreach (var instance in instances)
        {
            PerformanceCounter? item = null;
            try
            {
                item = new PerformanceCounter(category, counter, instance, readOnly: true);
                var value = item.NextValue();
                if (double.IsNaN(value) || double.IsInfinity(value))
                    continue;
                rows.Add((instance, value));
            }
            catch { }
            finally { item?.Dispose(); }
        }

        return rows;
    }

    private static bool TryPid(string instance, out int pid)
    {
        pid = 0;
        const string prefix = "pid_";
        var start = instance.IndexOf(prefix, StringComparison.OrdinalIgnoreCase);
        if (start < 0) return false;
        start += prefix.Length;
        var end = start;
        while (end < instance.Length && char.IsDigit(instance[end])) end++;
        return end > start && int.TryParse(instance[start..end], out pid);
    }

    private static string AdapterKey(string instance)
    {
        var luid = instance.IndexOf("luid_", StringComparison.OrdinalIgnoreCase);
        if (luid < 0) return instance;
        var phys = instance.IndexOf("_phys_", luid, StringComparison.OrdinalIgnoreCase);
        return phys > luid ? instance[luid..phys] : instance[luid..];
    }
}
