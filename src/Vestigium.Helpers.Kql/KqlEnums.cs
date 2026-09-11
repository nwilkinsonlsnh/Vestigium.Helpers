namespace Vestigium.Helpers.Kql;

public enum KqlPack
{
    Process = 0,
    Service = 1,
    Thread = 2,
    System = 3,
    Adapter = 4
}

[Flags]
public enum KqlGroups
{
    None = 0,
    Proc = 1,
    Cpu = 2,
    Gpu = 4,
    Mem = 8,
    Disk = 16,
    Io = 32,
    Net = 64,
    Svc = 128,
    Thr = 256,
    Sys = 512
}

public enum KqlType
{
    String = 0,
    Integer = 1,
    Number = 2,
    Boolean = 3,
    TimeSpan = 4,
    DateTime = 5
}
