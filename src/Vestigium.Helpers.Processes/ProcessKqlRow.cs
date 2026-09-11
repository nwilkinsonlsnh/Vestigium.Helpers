using Vestigium.Helpers.Kql;

namespace Vestigium.Helpers.Processes;

internal sealed class ProcessKqlRow : IKqlRow
{
    private readonly ProcessInfo _row;
    private readonly Dictionary<string, KqlValue> _overrides;

    public ProcessKqlRow(ProcessInfo row, IReadOnlyDictionary<string, object?>? extras = null)
    {
        _row = row;
        _overrides = extras is null
            ? new Dictionary<string, KqlValue>(StringComparer.OrdinalIgnoreCase)
            : extras.ToDictionary(pair => pair.Key, pair => KqlValue.From(pair.Value), StringComparer.OrdinalIgnoreCase);
    }

    public KqlValue Get(string canonical)
    {
        if (_overrides.TryGetValue(canonical, out var extra))
            return extra;

        return canonical switch
        {
            "PROC.Pid" => Value(_row.Pid),
            "PROC.ParentPid" => Maybe(_row.ParentPid, ProcessField.ParentPid),
            "PROC.Name" => Value(_row.Name),
            "PROC.ImagePath" => Maybe(_row.ImagePath, ProcessField.ImagePath),
            "PROC.CommandLine" => Maybe(_row.CommandLine, ProcessField.CommandLine),
            "PROC.Session" => Maybe(_row.SessionId, ProcessField.SessionId),
            "PROC.WindowTitle" => Maybe(_row.WindowTitle, ProcessField.WindowTitle),
            "PROC.Company" => Maybe(_row.CompanyName),
            "PROC.ImageType" => _row.ImageType == ProcessImageType.Unknown ? KqlValue.Unknown : Value(_row.ImageType.ToString()),
            "PROC.Integrity" => _row.IntegrityLevel is { } integrity ? Value(integrity.ToString()) : KqlValue.Unknown,
            "CPU.Usage" => Maybe(_row.CpuPercent, ProcessField.CpuPercent),
            "CPU.Time" => Maybe(_row.CpuTime, ProcessField.CpuTime),
            "MEM.PrivateBytes" => Maybe(_row.PrivateBytes, ProcessField.PrivateBytes),
            "MEM.WorkingSet" => Maybe(_row.WorkingSet, ProcessField.WorkingSet),
            "IO.Reads" => Maybe(_row.IoReads, ProcessField.IoReads),
            "IO.ReadBytes" => Maybe(_row.IoReadBytes, ProcessField.IoReadBytes),
            "IO.Writes" => Maybe(_row.IoWrites, ProcessField.IoWrites),
            "IO.WriteBytes" => Maybe(_row.IoWriteBytes, ProcessField.IoWriteBytes),
            "GPU.Usage" => Maybe(_row.GpuUsagePercent, ProcessField.GpuUsagePercent),
            "GPU.DedicatedMemory" => Maybe(_row.GpuDedicatedBytes, ProcessField.GpuDedicatedBytes),
            "GPU.SystemMemory" => Maybe(_row.GpuSystemBytes, ProcessField.GpuSystemBytes),
            _ => KqlValue.Unknown
        };
    }

    private KqlValue Maybe<T>(T? value, ProcessField? field = null) where T : struct
    {
        if (field is { } name && Denied(name))
            return KqlValue.Unknown;
        return value is { } present ? KqlValue.From(present) : KqlValue.Unknown;
    }

    private KqlValue Maybe(string? value, ProcessField? field = null)
    {
        if (field is { } name && Denied(name))
            return KqlValue.Unknown;
        return string.IsNullOrEmpty(value) ? KqlValue.Unknown : KqlValue.From(value);
    }

    private static KqlValue Value(object value) => KqlValue.From(value);

    private bool Denied(ProcessField field)
        => _row.Availability.Any(item => item.Field == field && item.State is Availability.Denied or Availability.Unsupported);
}
