using Vestigium.Helpers.Kql;

namespace Vestigium.Helpers.Processes;

internal sealed class ThreadKqlRow(ThreadInfo row) : IKqlRow
{
    public KqlValue Get(string canonical)
    {
        return canonical switch
        {
            "THR.Tid" => KqlValue.From(row.ThreadId),
            "THR.State" => row.State == ThreadState.Unknown ? KqlValue.Unknown : KqlValue.From(row.State.ToString()),
            "THR.StartAddress" => string.IsNullOrWhiteSpace(row.StartAddress) ? KqlValue.Unknown : KqlValue.From(row.StartAddress),
            "THR.Cpu" => row.CpuPercent is { } cpu ? KqlValue.From(cpu) : KqlValue.Unknown,
            "CPU.Usage" => row.CpuPercent is { } usage ? KqlValue.From(usage) : KqlValue.Unknown,
            _ => KqlValue.Unknown
        };
    }
}
