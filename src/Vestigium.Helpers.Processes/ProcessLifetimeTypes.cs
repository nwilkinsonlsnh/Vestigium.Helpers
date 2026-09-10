using System.Security;

namespace Vestigium.Helpers.Processes;

public sealed class ProcessStartRequest
{
    public required string FileName { get; init; }
    public string? Arguments { get; init; }
    public string? WorkingDirectory { get; init; }
    public bool UseShellExecute { get; init; }
    public bool CreateNoWindow { get; init; }
    public bool RedirectStandardIO { get; init; }
    public bool LogCommandLine { get; init; }
    public string? Verb { get; init; }
    public IReadOnlyDictionary<string, string>? Environment { get; init; }
}

public enum ProcessLogonFlags
{
    None = 0,
    WithProfile = 1,
    NetCredentialsOnly = 2
}

public sealed class ProcessStartAs
{
    public required string UserName { get; init; }
    public string? Domain { get; init; }
    public SecureString? Password { get; init; }
    public bool LoadUserProfile { get; init; }
    public ProcessLogonFlags LogonFlags { get; init; }
}

public enum ProcessStartError
{
    FileNotFound = 0,
    AccessDenied = 1,
    LogonFailed = 2,
    InvalidImage = 3,
    Cancelled = 4,
    Unknown = 5
}

public sealed class ProcessStartResult
{
    public bool Ok { get; init; }
    public int? Pid { get; init; }
    public ProcessStartError? Error { get; init; }
    public string? Message { get; init; }

    public static ProcessStartResult Success(int pid) => new() { Ok = true, Pid = pid };
    public static ProcessStartResult Fail(ProcessStartError error, string? message = null)
        => new() { Ok = false, Error = error, Message = message };
}

public enum ProcessKillStatus
{
    Ok = 0,
    Denied = 1,
    Gone = 2,
    Failed = 3
}

public sealed class ProcessKillResult
{
    public required int Pid { get; init; }
    public required ProcessKillStatus Status { get; init; }
    public string? Message { get; init; }
}

public sealed class ProcessSearchRequest
{
    public required string Term { get; init; }
    public ProcessSearchMode Mode { get; init; }
    public ProcessSearchFields Fields { get; init; } = ProcessSearchFields.Default;
}

public sealed class KillConfirm
{
    public const int Cap = 16;
    public required bool Confirm { get; init; }
    public int MaxResults { get; init; } = Cap;
}
