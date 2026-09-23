using System.Text;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;
using Vestigium.Helpers;
using Vestigium.Logging;

namespace Vestigium.Helpers.Xml;

/// <summary>
/// Owns one working tree and one committed tree. Not thread-safe — one session, one owner.
/// First / Count of a settings node are quiet. Mutations, snapshot, diff, commit, and save are audited.
/// </summary>
public sealed class XmlSession : IDisposable
{
    private const string App = HelperLog.AppIds.Xml;

    private XDocument _working;
    private XDocument _committed;
    private XDocument _saved;
    private XDocument? _snapshot;
    private string? _doctype;
    private bool _disposed;

    internal XmlSession(
        XDocument document,
        string? path,
        XmlEncodingResult encoding,
        XmlMediaType mediaType,
        string? doctype,
        XmlSessionOptions options,
        string sessionId)
    {
        var source = HelperGuard.NotNull(document, nameof(document));
        _committed = new XDocument(source);
        _working = new XDocument(source);
        _saved = new XDocument(source);
        _doctype = doctype;
        Path = path;
        EncodingName = encoding.Name;
        EncodingSource = encoding.Source;
        EncodingWarning = encoding.Warning;
        MediaType = mediaType;
        Options = options;
        SessionId = sessionId;
        HelperLog.Information(
            App,
            VestigiumStatus.Success,
            HelperLog.Subcategories.Session,
            $"created session={SessionId} path={Path ?? "(new)"} encoding={EncodingName}/{EncodingSource}");
    }

    public string SessionId { get; }

    public string? Path { get; private set; }

    public string RootName => _working.Root?.Name.LocalName ?? "(none)";

    public string DefaultNamespace => _working.Root?.GetDefaultNamespace().NamespaceName ?? string.Empty;

    public Encoding Encoding => XmlEncoding.ForName(EncodingName);

    public string EncodingName { get; }

    public string EncodingSource { get; }

    public string? EncodingWarning { get; }

    public XmlMediaType MediaType { get; }

    internal XmlSessionOptions Options { get; }

    public bool HasUncommittedWork => !XNode.DeepEquals(_working.Root, _committed.Root);

    public bool HasUnsavedCommit => !XNode.DeepEquals(_committed.Root, _saved.Root);

    public string? Doctype => _doctype;

    public void Snapshot()
    {
        ThrowIfDisposed();
        using var scope = HelperLog.Begin(App, HelperLog.Subcategories.Snapshot, "Snapshot", $"session={SessionId}", SessionId);
        _snapshot = new XDocument(_committed);
        HelperLog.Information(App, VestigiumStatus.Success, HelperLog.Subcategories.Snapshot, $"snapshot session={SessionId}");
    }

    public IReadOnlyList<XmlWorkNode> Search(XmlSearch query)
    {
        ThrowIfDisposed();
        var hits = Query(query);
        HelperLog.Information(
            App,
            VestigiumStatus.Success,
            HelperLog.Subcategories.Query,
            $"Search spelling={query.Spelling} hits={hits.Count} session={SessionId}");
        return hits;
    }

    public XmlWorkNode? First(XmlSearch query)
    {
        ThrowIfDisposed();
        var hits = Query(query);
        return hits.Count == 0 ? null : hits[0];
    }

    public int Count(XmlSearch query)
    {
        ThrowIfDisposed();
        return Query(query).Count;
    }

    public void SetText(string pathOrXPath, string? value)
    {
        ThrowIfDisposed();
        using var scope = HelperLog.Begin(App, HelperLog.Subcategories.Query, "SetText", "path=" + pathOrXPath, SessionId);
        try
        {
            var nodes = Select(pathOrXPath);
            if (nodes.Count == 0)
            {
                HelperLog.Reject(App, HelperLog.Subcategories.Query, "SetText", $"path not found path={pathOrXPath}", SessionId);
                throw new KeyNotFoundException($"XML path was not found: {pathOrXPath}.");
            }

            nodes[0].Value = value ?? string.Empty;
            HelperLog.Information(App, VestigiumStatus.Success, HelperLog.Subcategories.Query, $"SetText path={pathOrXPath} session={SessionId}");
        }
        catch (ArgumentException) { throw; }
        catch (KeyNotFoundException) { throw; }
        catch (XPathException) { throw; }
        catch (Exception ex)
        {
            HelperLog.Trap(ex);
            throw;
        }
    }

    public void SetAttribute(string pathOrXPath, string attributeName, string? value)
    {
        ThrowIfDisposed();
        var name = HelperGuard.NotBlank(attributeName, nameof(attributeName));
        using var scope = HelperLog.Begin(App, HelperLog.Subcategories.Query, "SetAttribute", "path=" + pathOrXPath, SessionId);
        try
        {
            var nodes = Select(pathOrXPath);
            if (nodes.Count == 0)
            {
                HelperLog.Reject(App, HelperLog.Subcategories.Query, "SetAttribute", $"path not found path={pathOrXPath}", SessionId);
                throw new KeyNotFoundException($"XML path was not found: {pathOrXPath}.");
            }

            nodes[0].SetAttributeValue(name, value ?? string.Empty);
            HelperLog.Information(
                App,
                VestigiumStatus.Success,
                HelperLog.Subcategories.Query,
                $"SetAttribute path={pathOrXPath} name={name} session={SessionId}");
        }
        catch (ArgumentException) { throw; }
        catch (KeyNotFoundException) { throw; }
        catch (XPathException) { throw; }
        catch (Exception ex)
        {
            HelperLog.Trap(ex);
            throw;
        }
    }

    public void Insert(string parentPath, XElement child)
    {
        ThrowIfDisposed();
        var node = HelperGuard.NotNull(child, nameof(child));
        using var scope = HelperLog.Begin(App, HelperLog.Subcategories.Query, "Insert", "path=" + parentPath, SessionId);
        try
        {
            if (parentPath is "/" or "")
            {
                if (_working.Root is null)
                    _working.Add(new XElement(node));
                else
                    _working.Root.ReplaceWith(new XElement(node));
                HelperLog.Information(App, VestigiumStatus.Success, HelperLog.Subcategories.Query, $"Insert path=/ session={SessionId}");
                return;
            }

            var parents = Select(parentPath);
            if (parents.Count == 0)
            {
                HelperLog.Reject(App, HelperLog.Subcategories.Query, "Insert", $"path not found path={parentPath}", SessionId);
                throw new KeyNotFoundException($"XML path was not found: {parentPath}.");
            }

            parents[0].Add(new XElement(node));
            HelperLog.Information(App, VestigiumStatus.Success, HelperLog.Subcategories.Query, $"Insert path={parentPath} session={SessionId}");
        }
        catch (ArgumentException) { throw; }
        catch (KeyNotFoundException) { throw; }
        catch (Exception ex)
        {
            HelperLog.Trap(ex);
            throw;
        }
    }

    public void Delete(string pathOrXPath)
    {
        ThrowIfDisposed();
        using var scope = HelperLog.Begin(App, HelperLog.Subcategories.Query, "Delete", "path=" + pathOrXPath, SessionId);
        try
        {
            var nodes = Select(pathOrXPath);
            if (nodes.Count == 0)
            {
                HelperLog.Reject(App, HelperLog.Subcategories.Query, "Delete", $"path not found path={pathOrXPath}", SessionId);
                throw new KeyNotFoundException($"XML path was not found: {pathOrXPath}.");
            }

            var el = nodes[0];
            if (ReferenceEquals(el, _working.Root))
            {
                HelperLog.Reject(App, HelperLog.Subcategories.Query, "Delete", "refuse to delete the document root", SessionId);
                throw new InvalidOperationException("Refuse to delete the document root; replace it with Insert(\"/\").");
            }

            el.Remove();
            HelperLog.Information(App, VestigiumStatus.Success, HelperLog.Subcategories.Query, $"Delete path={pathOrXPath} session={SessionId}");
        }
        catch (ArgumentException) { throw; }
        catch (InvalidOperationException) { throw; }
        catch (KeyNotFoundException) { throw; }
        catch (Exception ex)
        {
            HelperLog.Trap(ex);
            throw;
        }
    }

    public IReadOnlyList<XmlChange> Diff()
    {
        ThrowIfDisposed();
        using var scope = HelperLog.Begin(App, HelperLog.Subcategories.Diff, "Diff", $"session={SessionId}", SessionId);
        var baseline = _snapshot ?? _committed;
        var changes = Compare(baseline, _working);
        HelperLog.Information(App, VestigiumStatus.Success, HelperLog.Subcategories.Diff, $"ops={changes.Count} session={SessionId}");
        return changes;
    }

    public void Commit()
    {
        ThrowIfDisposed();
        using var scope = HelperLog.Begin(App, HelperLog.Subcategories.Commit, "Commit", $"session={SessionId}", SessionId);
        _committed = new XDocument(_working);
        HelperLog.Information(App, VestigiumStatus.Success, HelperLog.Subcategories.Commit, $"Commit session={SessionId}");
    }

    public void Revert()
    {
        ThrowIfDisposed();
        using var scope = HelperLog.Begin(App, HelperLog.Subcategories.Commit, "Revert", $"session={SessionId}", SessionId);
        _working = new XDocument(_snapshot ?? _committed);
        HelperLog.Information(App, VestigiumStatus.Success, HelperLog.Subcategories.Commit, $"Revert session={SessionId}");
    }

    public void Cancel()
    {
        ThrowIfDisposed();
        using var scope = HelperLog.Begin(App, HelperLog.Subcategories.Commit, "Cancel", $"session={SessionId}", SessionId);
        _working = new XDocument(_committed);
        HelperLog.Information(App, VestigiumStatus.Success, HelperLog.Subcategories.Commit, $"Cancel session={SessionId}");
    }

    public string Save()
    {
        ThrowIfDisposed();
        if (string.IsNullOrWhiteSpace(Path))
            return SaveAs(XmlHelper.NewExportPath(), Options.Collision);
        return WriteTree(_committed, Path, replaceInPlace: true, Options.Collision, working: false, "Save");
    }

    public string SaveWorking()
    {
        ThrowIfDisposed();
        var target = string.IsNullOrWhiteSpace(Path) ? XmlHelper.NewExportPath() : Path;
        var replace = XmlIo.SamePath(Path, target);
        return WriteTree(_working, target, replace, Options.Collision, working: true, "SaveWorking");
    }

    public string SaveAs(string path, XmlCollision collision = XmlCollision.Fail)
    {
        ThrowIfDisposed();
        var target = System.IO.Path.GetFullPath(HelperGuard.NotBlank(path, nameof(path)));
        var replace = XmlIo.SamePath(Path, target);
        return WriteTree(_committed, target, replace, collision, working: false, "SaveAs");
    }

    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;
        HelperLog.Information(App, VestigiumStatus.Success, HelperLog.Subcategories.Session, $"disposed session={SessionId}");
    }

    public string WorkingXml(bool indent = false)
    {
        ThrowIfDisposed();
        return XmlIo.Serialize(_working, _doctype, indent);
    }

    private string WriteTree(XDocument tree, string path, bool replaceInPlace, XmlCollision collision, bool working, string method)
    {
        using var scope = HelperLog.Begin(App, HelperLog.Subcategories.Save, method, $"path={path} session={SessionId}", SessionId);
        try
        {
            var options = new XmlWriteOptions
            {
                Indent = Options.Indent,
                EmitBom = Options.EmitBom,
                Collision = collision,
                AtomicWrite = Options.AtomicWrite
            };
            var bytes = XmlIo.WriteExisting(path, tree, _doctype, options, replaceInPlace);
            Path = path;
            _saved = new XDocument(tree);
            if (working)
            {
                HelperLog.Warning(
                    App,
                    VestigiumStatus.Success,
                    HelperLog.Subcategories.Save,
                    $"SaveWorking path={path} bytes={bytes} collision={collision} atomic={Options.AtomicWrite} session={SessionId}");
            }
            else
            {
                HelperLog.Information(
                    App,
                    VestigiumStatus.Success,
                    HelperLog.Subcategories.Save,
                    $"Saved path={path} bytes={bytes} collision={collision} atomic={Options.AtomicWrite} session={SessionId}");
            }

            return path;
        }
        catch (ArgumentException) { throw; }
        catch (IOException) { throw; }
        catch (Exception ex)
        {
            HelperLog.Trap(ex);
            throw;
        }
    }

    private void ThrowIfDisposed() => HelperGuard.NotDisposed(_disposed, this);

    private IReadOnlyList<XmlWorkNode> Query(XmlSearch query)
    {
        var q = HelperGuard.NotNull(query, nameof(query));
        var root = _working.Root;
        if (root is null)
            return [];

        if (q.Kind == "xpath")
        {
            var xpath = q.XPathText!;
            try
            {
                var nsm = CreateNs(q.Namespaces);
                var selected = _working.XPathSelectElements(xpath, nsm).ToList();
                return selected.Select(ToWorkNode).ToList();
            }
            catch (XPathException)
            {
                HelperLog.Reject(App, HelperLog.Subcategories.Query, "Search", $"xpath is not valid spelling={xpath}", SessionId);
                throw;
            }
        }

        var hits = new List<XmlWorkNode>();
        foreach (var el in root.DescendantsAndSelf())
        {
            if (q.Ancestor is not null && !el.Ancestors().Any(a => a.Name.LocalName == q.Ancestor))
                continue;

            if (q.Kind == "name")
            {
                if (el.Name.LocalName != q.LocalName)
                    continue;
                if (q.NamespaceUri is not null && el.Name.NamespaceName != q.NamespaceUri)
                    continue;
                if (q.Text is not null && !XmlSearch.Matches(ElementText(el), q.Text, q.Match, q.CaseInsensitive))
                    continue;
                hits.Add(ToWorkNode(el));
                continue;
            }

            if (q.Kind == "text")
            {
                if (q.Text is not null && XmlSearch.Matches(ElementText(el), q.Text, q.Match, q.CaseInsensitive))
                    hits.Add(ToWorkNode(el));
                continue;
            }

            if (q.Kind == "attribute")
            {
                var attr = el.Attributes().FirstOrDefault(a => !a.IsNamespaceDeclaration && a.Name.LocalName == q.AttributeName);
                if (attr is null)
                    continue;
                if (q.AttributeValue is not null && !XmlSearch.Matches(attr.Value, q.AttributeValue, q.Match, q.CaseInsensitive))
                    continue;
                hits.Add(ToWorkNode(el));
            }
        }

        return hits;
    }

    private List<XElement> Select(string pathOrXPath)
    {
        var xpath = HelperGuard.NotBlank(pathOrXPath, nameof(pathOrXPath));
        if (_working.Root is null)
            return [];
        try
        {
            return _working.XPathSelectElements(xpath, CreateNs([])).ToList();
        }
        catch (XPathException)
        {
            HelperLog.Reject(App, HelperLog.Subcategories.Query, "Select", $"xpath is not valid spelling={xpath}", SessionId);
            throw;
        }
    }

    private XmlNamespaceManager CreateNs(IReadOnlyList<XmlNs> extra)
    {
        var table = new NameTable();
        var nsm = new XmlNamespaceManager(table);
        if (_working.Root is { } root)
        {
            var def = root.GetDefaultNamespace().NamespaceName;
            if (!string.IsNullOrEmpty(def))
            {
                nsm.AddNamespace("u", def);
                nsm.AddNamespace("d", def);
            }

            foreach (var attr in root.Attributes().Where(a => a.IsNamespaceDeclaration))
            {
                var prefix = attr.Name.LocalName == "xmlns" ? "u" : attr.Name.LocalName;
                if (!nsm.HasNamespace(prefix))
                    nsm.AddNamespace(prefix, attr.Value);
            }
        }

        foreach (var ns in extra)
            nsm.AddNamespace(ns.Prefix, ns.Uri);
        return nsm;
    }

    private static XmlWorkNode ToWorkNode(XElement el) => new(el, ElementPath(el));

    private static string ElementPath(XElement el)
    {
        var parts = new List<string>();
        var cur = el;
        while (cur is not null)
        {
            var name = cur.Name.LocalName;
            var index = 1;
            var sib = cur.ElementsBeforeSelf().Count(s => s.Name.LocalName == name);
            index += sib;
            parts.Add($"{name}[{index}]");
            cur = cur.Parent;
        }
        parts.Reverse();
        return "/" + string.Join("/", parts);
    }

    private static string ElementText(XElement el)
    {
        if (el.HasElements)
            return string.Empty;
        return (el.Value ?? string.Empty).Trim();
    }

    private static List<XmlChange> Compare(XDocument baseline, XDocument working)
    {
        var a = Index(baseline.Root);
        var b = Index(working.Root);
        var changes = new List<XmlChange>();
        foreach (var (path, now) in b)
        {
            if (!a.TryGetValue(path, out var was))
            {
                changes.Add(new XmlChange(path.Count(c => c == '/') <= 1 ? "replace-root" : "insert", path, now.Name));
                continue;
            }
            if (was.Text != now.Text)
                changes.Add(new XmlChange("set-text", path, now.Name));
            foreach (var key in was.Attrs.Keys.Union(now.Attrs.Keys))
            {
                was.Attrs.TryGetValue(key, out var left);
                now.Attrs.TryGetValue(key, out var right);
                if (left != right)
                    changes.Add(new XmlChange("set-attribute", path, key));
            }
        }
        foreach (var (path, was) in a)
        {
            if (!b.ContainsKey(path))
                changes.Add(new XmlChange("delete", path, was.Name));
        }
        return changes;
    }

    private static Dictionary<string, (string Name, string Text, Dictionary<string, string> Attrs)> Index(XElement? root)
    {
        var map = new Dictionary<string, (string, string, Dictionary<string, string>)>(StringComparer.Ordinal);
        if (root is null)
            return map;
        foreach (var el in root.DescendantsAndSelf())
        {
            var path = ElementPath(el);
            map[path] = (
                el.Name.LocalName,
                ElementText(el),
                el.Attributes().Where(a => !a.IsNamespaceDeclaration)
                    .ToDictionary(a => a.Name.LocalName, a => a.Value, StringComparer.Ordinal));
        }
        return map;
    }
}
