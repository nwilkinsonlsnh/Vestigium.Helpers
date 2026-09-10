namespace Vestigium.Helpers.Processes;

public enum ProcessDetailLevel
{
    Identity = 0,
    Slim = 1,
    Full = 2
}

public enum ProcessSearchMode
{
    StartsWith = 0,
    EndsWith = 1,
    Contains = 2
}

public enum Availability
{
    Available = 0,
    Denied = 1,
    Unsupported = 2,
    Gone = 3
}

public enum ProcessField
{
    Pid = 0,
    ParentPid = 1,
    Name = 2,
    SessionId = 3,
    ImagePath = 4,
    CpuTime = 5,
    CpuPercent = 6,
    PrivateBytes = 7,
    WorkingSet = 8,
    IoReads = 9,
    IoReadBytes = 10,
    IoWrites = 11,
    IoWriteBytes = 12,
    GpuUsagePercent = 13,
    GpuDedicatedBytes = 14,
    GpuSystemBytes = 15,
    CommandLine = 16,
    WindowTitle = 17,
    IntegrityLevel = 18,
    DepStatus = 19,
    AslrEnabled = 20,
    DpiAwareness = 21,
    VerifiedSigner = 22
}

[Flags]
public enum ProcessSearchFields
{
    Name = 1,
    ImagePath = 2,
    CommandLine = 4,
    WindowTitle = 8,
    Default = Name | ImagePath | CommandLine | WindowTitle
}

public enum ProcessImageType
{
    Unknown = 0,
    X86 = 1,
    X64 = 2,
    Arm64 = 3
}
