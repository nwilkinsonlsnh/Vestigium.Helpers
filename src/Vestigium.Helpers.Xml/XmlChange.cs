namespace Vestigium.Helpers.Xml;

public sealed class XmlChange
{
    public XmlChange(string op, string path, string localName)
    {
        Op = op;
        Path = path;
        LocalName = localName;
    }

    public string Op { get; }

    public string Path { get; }

    public string LocalName { get; }

    public override string ToString() => Op + " " + Path;
}
