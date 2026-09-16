namespace Vestigium.Helpers.Xml;

public enum XmlCollision
{
    Fail = 0,
    Overwrite = 1
}

public sealed class XmlReadOptions
{
    public bool ProhibitDtd { get; init; } = true;
    public bool MultiDocument { get; init; } = false;
    public string? Charset { get; init; }
    public long MaxCharacters { get; init; } = 4_000_000;
}

public sealed class XmlWriteOptions
{
    public bool Indent { get; init; } = false;
    public bool EmitBom { get; init; } = false;
    public XmlCollision Collision { get; init; } = XmlCollision.Fail;
    public bool AtomicWrite { get; init; } = true;
}

public sealed class XmlSessionOptions
{
    public XmlCollision Collision { get; init; } = XmlCollision.Fail;
    public bool AtomicWrite { get; init; } = true;
    public bool Indent { get; init; } = false;
    public bool EmitBom { get; init; } = false;
    public string? Charset { get; init; }
    public long MaxCharacters { get; init; } = 4_000_000;
    public bool ProhibitDtd { get; init; } = true;
}
