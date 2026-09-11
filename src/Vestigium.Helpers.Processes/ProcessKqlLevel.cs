using Vestigium.Helpers.Kql;

namespace Vestigium.Helpers.Processes;

internal static class ProcessKqlLevel
{
    private static readonly HashSet<string> SlimEnough = new(StringComparer.OrdinalIgnoreCase)
    {
        "PROC.Pid", "PROC.ParentPid", "PROC.Name", "PROC.Session",
        "PROC.ImagePath",
        "CPU.Usage", "CPU.Time", "CPU.UserTime", "CPU.KernelTime", "CPU.TimeDelta", "CPU.Priority",
        "MEM.PrivateBytes", "MEM.WorkingSet", "MEM.Commit", "MEM.VirtualBytes",
        "MEM.WorkingSetPeak", "MEM.PageFaults", "MEM.PageFaultDelta",
        "MEM.PrivateBytesDelta", "MEM.WorkingSetDelta",
        "IO.Reads", "IO.ReadBytes", "IO.Writes", "IO.WriteBytes",
        "IO.ReadDelta", "IO.WriteDelta", "IO.Other", "IO.OtherBytes",
        "IO.OtherDelta", "IO.OtherBytesDelta", "IO.ReadBytesDelta", "IO.WriteBytesDelta", "IO.BytesPerSec",
        "GPU.Usage", "GPU.DedicatedMemory", "GPU.SystemMemory", "GPU.CommittedMemory", "GPU.Adapter", "GPU.Engine"
    };

    public static ProcessDetailLevel Resolve(ProcessDetailLevel requested, KqlExpression expression, KqlSession session)
    {
        if (requested == ProcessDetailLevel.Full)
            return ProcessDetailLevel.Full;
        return NeedsFull(expression, session) ? ProcessDetailLevel.Full : Max(requested, ProcessDetailLevel.Slim);
    }

    internal static bool NeedsFull(KqlExpression expression, KqlSession session)
    {
        foreach (var name in Fields(expression))
        {
            if (!session.TryGetField(name, out var field))
                continue;
            if (!SlimEnough.Contains(field.Canonical))
                return true;
        }

        return false;
    }

    private static ProcessDetailLevel Max(ProcessDetailLevel a, ProcessDetailLevel b)
        => (ProcessDetailLevel)Math.Max((int)a, (int)b);

    private static IEnumerable<string> Fields(KqlExpression expr)
    {
        switch (expr)
        {
            case KqlLogicalExpression logical:
                foreach (var name in Fields(logical.Left))
                    yield return name;
                foreach (var name in Fields(logical.Right))
                    yield return name;
                break;
            case KqlNotExpression not:
                foreach (var name in Fields(not.Operand))
                    yield return name;
                break;
            case KqlComparisonExpression cmp:
                yield return cmp.Field;
                break;
        }
    }
}
