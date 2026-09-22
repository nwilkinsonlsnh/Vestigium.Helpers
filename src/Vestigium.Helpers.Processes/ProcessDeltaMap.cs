namespace Vestigium.Helpers.Processes;

internal sealed class ProcessDeltaMap
{
    private readonly Dictionary<int, ProcessInfo> _prior = [];

    public IReadOnlyDictionary<string, object?> Remember(ProcessInfo row, TimeSpan interval)
    {
        _prior.TryGetValue(row.Pid, out var prior);
        var extras = Diff(row, prior, interval);
        _prior[row.Pid] = row;
        return extras;
    }

    public void Clear() => _prior.Clear();

    public void Prune(IReadOnlySet<int> live)
    {
        var dead = _prior.Keys.Where(pid => !live.Contains(pid)).ToArray();
        foreach (var pid in dead)
            _prior.Remove(pid);
    }

    internal static IReadOnlyDictionary<string, object?> Diff(ProcessInfo row, ProcessInfo? prior, TimeSpan interval)
    {
        var extras = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        if (prior is null)
            return extras;

        if (row.CpuTime is { } nowCpu && prior.CpuTime is { } thenCpu)
        {
            var delta = nowCpu - thenCpu;
            extras["CPU.TimeDelta"] = delta;
            var seconds = interval.TotalSeconds * Math.Max(1, Environment.ProcessorCount);
            if (seconds > 0)
                extras["CPU.Usage"] = 100.0 * Math.Max(0, delta.TotalSeconds) / seconds;
        }

        Put(extras, "MEM.PrivateBytesDelta", row.PrivateBytes, prior.PrivateBytes);
        Put(extras, "MEM.WorkingSetDelta", row.WorkingSet, prior.WorkingSet);
        Put(extras, "IO.ReadDelta", row.IoReads, prior.IoReads);
        Put(extras, "IO.ReadBytesDelta", row.IoReadBytes, prior.IoReadBytes);
        Put(extras, "IO.WriteDelta", row.IoWrites, prior.IoWrites);
        Put(extras, "IO.WriteBytesDelta", row.IoWriteBytes, prior.IoWriteBytes);
        return extras;
    }

    private static void Put(Dictionary<string, object?> extras, string name, long? now, long? then)
    {
        if (now is { } a && then is { } b)
            extras[name] = a - b;
    }
}
