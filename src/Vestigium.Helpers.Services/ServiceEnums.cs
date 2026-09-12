namespace Vestigium.Helpers.Services;

public enum ServiceDetailLevel
{
    Identity = 0,
    Slim = 1,
    Full = 2
}

public enum ServiceKind
{
    Win32 = 0,
    Driver = 1,
    All = 2
}

public enum ServiceListScope
{
    Visible = 0,
    Hidden = 1,
    All = 2
}

public enum ServiceSearchMode
{
    StartsWith = 0,
    EndsWith = 1,
    Contains = 2
}

[Flags]
public enum ServiceSearchFields
{
    None = 0,
    Name = 1,
    DisplayName = 2,
    Description = 4,
    ImagePath = 8,
    Account = 16,
    Default = Name | DisplayName | Description | ImagePath
}

public enum ServiceStatus
{
    Stopped = 1,
    StartPending = 2,
    StopPending = 3,
    Running = 4,
    ContinuePending = 5,
    PausePending = 6,
    Paused = 7,
    Unknown = 0
}

public enum ServiceStartType
{
    Boot = 0,
    System = 1,
    Automatic = 2,
    Manual = 3,
    Disabled = 4,
    AutomaticDelayed = 5
}

public enum ServiceErrorControl
{
    Ignore = 0,
    Normal = 1,
    Severe = 2,
    Critical = 3
}

[Flags]
public enum ServiceTypeFlags
{
    None = 0,
    KernelDriver = 0x00000001,
    FileSystemDriver = 0x00000002,
    Adapter = 0x00000004,
    RecognizerDriver = 0x00000008,
    Win32OwnProcess = 0x00000010,
    Win32ShareProcess = 0x00000020,
    UserService = 0x00000040,
    UserServiceInstance = 0x00000080,
    InteractiveProcess = 0x00000100,
    PkgService = 0x00000200
}

[Flags]
public enum ServiceControls
{
    None = 0,
    Stop = 0x00000001,
    PauseContinue = 0x00000002,
    Shutdown = 0x00000004,
    ParamChange = 0x00000008,
    NetBindChange = 0x00000010,
    HardwareProfileChange = 0x00000020,
    PowerEvent = 0x00000040,
    SessionChange = 0x00000080,
    Preshutdown = 0x00000100
}

public enum ServiceSidType
{
    None = 0,
    Unrestricted = 1,
    Restricted = 2
}

public enum ServiceLaunchProtected
{
    None = 0,
    Windows = 1,
    WindowsLight = 2,
    AntimalwareLight = 3
}

public enum ServiceFailureActionKind
{
    None = 0,
    Restart = 1,
    Reboot = 2,
    RunCommand = 3
}

public enum ServiceControlStatus
{
    Ok = 0,
    Denied = 1,
    NotFound = 2,
    InvalidState = 3,
    Timeout = 4,
    DependentRunning = 5,
    HasDependents = 6,
    Unsupported = 7,
    Failed = 8
}

public enum ServiceLogonKind
{
    LocalSystem = 0,
    LocalService = 1,
    NetworkService = 2,
    Account = 3
}

[Flags]
public enum ServiceGrantLogonRight
{
    None = 0,
    Service = 1,
    Batch = 2,
    ServiceAndBatch = Service | Batch
}

public enum ServiceField
{
    Name,
    DisplayName,
    Description,
    Status,
    Pid,
    StartType,
    ImagePath,
    Account,
    DependsOn,
    FailureActions,
    Triggers
}

public enum ServiceTreeDirection
{
    DependsOn = 0,
    DependedBy = 1,
    Both = 2
}

[Flags]
public enum ServiceWatchFields
{
    None = 0,
    Status = 1,
    Pid = 2,
    Process = 4
}
