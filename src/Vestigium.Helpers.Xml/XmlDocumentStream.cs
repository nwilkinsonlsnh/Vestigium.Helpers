using System.Collections;

namespace Vestigium.Helpers.Xml;

public sealed class XmlDocumentStream : IReadOnlyList<XmlSession>, IDisposable
{
    private readonly XmlSession[] _sessions;
    private bool _disposed;

    internal XmlDocumentStream(IEnumerable<XmlSession> sessions, string path)
    {
        _sessions = [.. sessions];
        Path = path;
    }

    public string Path { get; }

    public int Count => _sessions.Length;

    public XmlSession this[int index] => _sessions[index];

    public IEnumerator<XmlSession> GetEnumerator() => ((IEnumerable<XmlSession>)_sessions).GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;
        foreach (var session in _sessions)
            session.Dispose();
    }
}
