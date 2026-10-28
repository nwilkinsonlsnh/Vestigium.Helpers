namespace Vestigium.Helpers.Xml;

public sealed class XmlChange(string op, string path, string localName)
{
    public string Op { get; } = op;

    public string Path { get; } = path;

    public string LocalName { get; } = localName;

    public override string ToString() => Op + " " + Path;
}
