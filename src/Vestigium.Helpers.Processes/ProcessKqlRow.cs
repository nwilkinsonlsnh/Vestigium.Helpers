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
            "PROC.WindowStatus" => _row.WindowStatus == WindowStatus.None ? KqlValue.Unknown : Value(_row.WindowStatus.ToString()),
            "PROC.Company" => Maybe(_row.CompanyName),
            "PROC.Description" => Maybe(_row.Description),
            "PROC.Version" => Maybe(_row.Version),
            "PROC.Signer" => Maybe(_row.VerifiedSigner?.Publisher, ProcessField.VerifiedSigner),
            "PROC.SignerTrust" => _row.VerifiedSigner is { } signer ? Value(signer.Trust.ToString()) : KqlValue.Unknown,
            "PROC.Package" => Maybe(_row.PackageName),
            "PROC.Autostart" => Maybe(_row.AutostartLocation),
            "PROC.Comment" => Maybe(_row.Comment),
            "PROC.ImageType" => _row.ImageType == ProcessImageType.Unknown ? KqlValue.Unknown : Value(_row.ImageType.ToString()),
            "PROC.Integrity" => _row.IntegrityLevel is { } integrity ? Value(integrity.ToString()) : KqlValue.Unknown,
            "PROC.Dep" => _row.DepStatus is { } dep && dep != DepStatus.Unknown ? Value(dep.ToString()) : KqlValue.Unknown,
            "PROC.Aslr" => Maybe(_row.AslrEnabled, ProcessField.AslrEnabled),
            "PROC.Cfg" => EnumOrUnknown(_row.ControlFlowGuard),
            "PROC.StackProtection" => EnumOrUnknown(_row.StackProtection),
            "PROC.Protection" => string.IsNullOrWhiteSpace(_row.Protection?.Level) ? KqlValue.Unknown : Value(_row.Protection!.Value.Level),
            "PROC.Dpi" => _row.DpiAwareness is { } dpi ? Value(dpi.ToString()) : KqlValue.Unknown,
            "PROC.UiAccess" => Maybe(_row.UiAccess),
            "PROC.Virtualized" => Maybe(_row.Virtualized),
            "PROC.EnterpriseContext" => Maybe(_row.EnterpriseContext),
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

    private static KqlValue EnumOrUnknown(MitigationState? state)
        => state is { } value && value != MitigationState.Unknown ? KqlValue.From(value.ToString()) : KqlValue.Unknown;

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
        return string.IsNullOrWhiteSpace(value) ? KqlValue.Unknown : KqlValue.From(value);
    }

    private static KqlValue Value(object value) => KqlValue.From(value);

    private bool Denied(ProcessField field)
        => _row.Availability.Any(item => item.Field == field && item.State is Availability.Denied or Availability.Unsupported or Availability.Gone);
}
