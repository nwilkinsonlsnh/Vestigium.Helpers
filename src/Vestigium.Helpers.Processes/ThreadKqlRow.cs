using Vestigium.Helpers.Kql;

namespace Vestigium.Helpers.Processes;

internal sealed class ThreadKqlRow : IKqlRow
{
    private readonly ThreadInfo _row;

    public ThreadKqlRow(ThreadInfo row) => _row = row;

    public KqlValue Get(string canonical)
    {
        return canonical switch
        {
            "THR.Tid" => KqlValue.From(_row.ThreadId),
            "THR.State" => _row.State == ThreadState.Unknown ? KqlValue.Unknown : KqlValue.From(_row.State.ToString()),
            "THR.StartAddress" => string.IsNullOrWhiteSpace(_row.StartAddress) ? KqlValue.Unknown : KqlValue.From(_row.StartAddress),
            "THR.Cpu" => _row.CpuPercent is { } cpu ? KqlValue.From(cpu) : KqlValue.Unknown,
            "CPU.Usage" => _row.CpuPercent is { } usage ? KqlValue.From(usage) : KqlValue.Unknown,
            _ => KqlValue.Unknown
        };
    }
}
