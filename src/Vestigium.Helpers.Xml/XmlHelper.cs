using System.Text;
using System.Xml;
using System.Xml.Linq;
using Vestigium.Helpers;
using Vestigium.Logging;

namespace Vestigium.Helpers.Xml;

/// <summary>
/// XML document helpers for payload files. Platform parser only (XmlReader + XDocument).
/// RFC 7303 encoding and media types. The class library never calls <see cref="VestigiumLogger.Initialize"/>.
/// </summary>
public static class XmlHelper
{
    public static string Identity => "Vestigium.Helpers.Xml";

    public static string ContentType() => XmlMediaType.ApplicationXml.ToContentType();

    public static bool IsXmlFamily(string mediaType) => XmlMediaType.IsXmlFamily(mediaType);

    public static string Probe()
    {
        const string app = HelperLog.AppIds.Xml;
        using var scope = HelperLog.Begin(app, HelperLog.Subcategories.Probe, "Probe");
        HelperLog.Information(app, VestigiumStatus.Pending, HelperLog.Subcategories.Probe, "Building a demo XML document.");
        _ = Parse("<probe identity=\"Vestigium.Helpers.Xml\"/>");
        HelperLog.Information(app, VestigiumStatus.Success, HelperLog.Subcategories.Probe, "XML probe complete. Identity=" + Identity);
        HelperLog.Exit(app, HelperLog.Subcategories.Probe, "Probe", "Identity=" + Identity);
        return Identity;
    }

    public static string DefaultExportDirectory()
    {
        if (!string.IsNullOrWhiteSpace(XmlTestHooks.ExportRoot))
            return Path.GetFullPath(XmlTestHooks.ExportRoot);

        var desktop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
        if (string.IsNullOrWhiteSpace(desktop))
            desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
        if (!string.IsNullOrWhiteSpace(desktop)) return Path.Combine(desktop, "Vestigium", "Exports", "Xml");
        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        desktop = Path.Combine(string.IsNullOrWhiteSpace(home) ? "." : home, "Desktop");

        return Path.Combine(desktop, "Vestigium", "Exports", "Xml");
    }

    public static string NewExportPath(string? stem = null)
    {
        var stamp = DateTime.Now.ToString("yyyyMMdd-HHmmss");
        var name = string.IsNullOrWhiteSpace(stem)
            ? $"vestigium-Xml-{stamp}"
            : stem.Trim();
        return XmlIo.ResolveExportFile(DefaultExportDirectory(), name);
    }

    public static XDocument Parse(string xml, XmlReadOptions? options = null)
    {
        const string app = HelperLog.AppIds.Xml;
        using var scope = HelperLog.Begin(app, HelperLog.Subcategories.Document, "Parse");
        try
        {
            var text = HelperGuard.NotBlank(xml, nameof(xml));
            var opts = options ?? new XmlReadOptions();
            GuardSize(text.Length, opts);
            var (document, _) = XmlIo.LoadDocument(text, opts);
            HelperLog.Information(
                app,
                VestigiumStatus.Success,
                HelperLog.Subcategories.Document,
                $"Parse root={document.Root!.Name.LocalName} chars={text.Length}");
            return document;
        }
        catch (ArgumentException) { throw; }
        catch (XmlException) { throw; }
        catch (Exception ex)
        {
            HelperLog.Trap(ex);
            throw;
        }
    }

    public static XDocument Parse(Stream stream, XmlReadOptions? options = null)
    {
        const string app = HelperLog.AppIds.Xml;
        using var scope = HelperLog.Begin(app, HelperLog.Subcategories.Document, "ParseStream");
        try
        {
            var input = HelperGuard.NotNull(stream, nameof(stream));
            HelperGuard.Require(input.CanRead, nameof(stream), "Stream must be readable.");
            using var memory = new MemoryStream();
            input.CopyTo(memory);
            var bytes = memory.ToArray();
            var opts = options ?? new XmlReadOptions();
            GuardSize(bytes.Length, opts);
            var detected = XmlEncoding.Detect(bytes, opts.Charset);
            LogEncoding(detected);
            var text = XmlEncoding.Decode(bytes, detected);
            var (document, _) = XmlIo.LoadDocument(text, opts);
            HelperLog.Information(
                app,
                VestigiumStatus.Success,
                HelperLog.Subcategories.Document,
                $"ParseStream root={document.Root!.Name.LocalName} bytes={bytes.Length} encoding={detected.Name}");
            return document;
        }
        catch (ArgumentException) { throw; }
        catch (XmlException) { throw; }
        catch (Exception ex)
        {
            HelperLog.Trap(ex);
            throw;
        }
    }

    public static XmlSession Create(string? path = null, XmlSessionOptions? options = null)
    {
        const string app = HelperLog.AppIds.Xml;
        var sessionId = HelperLog.NewId();
        using var scope = HelperLog.Begin(app, HelperLog.Subcategories.Session, "Create", $"session={sessionId}", sessionId);
        try
        {
            var stored = string.IsNullOrWhiteSpace(path) ? null : Path.GetFullPath(path.Trim());
            var document = new XDocument(new XDeclaration("1.0", "utf-8", null), new XElement("root"));
            var encoding = new XmlEncodingResult
            {
                Encoding = new UTF8Encoding(false),
                Name = "utf-8",
                Source = "utf-8"
            };
            return new XmlSession(
                document,
                stored,
                encoding,
                XmlMediaType.ApplicationXml,
                doctype: null,
                options ?? new XmlSessionOptions(),
                sessionId);
        }
        catch (Exception ex)
        {
            HelperLog.Trap(ex);
            throw;
        }
    }

    public static XmlSession Open(string path, XmlSessionOptions? options = null)
    {
        const string app = HelperLog.AppIds.Xml;
        var sessionId = HelperLog.NewId();
        var target = Path.GetFullPath(HelperGuard.FileExists(path, nameof(path)));
        using var scope = HelperLog.Begin(app, HelperLog.Subcategories.Session, "Open", $"path={target} session={sessionId}", sessionId);
        try
        {
            var bytes = XmlIo.ReadAllBytes(target);
            var sessionOptions = options ?? new XmlSessionOptions();
            var read = ToReadOptions(sessionOptions);
            GuardSize(bytes.Length, read);
            var detected = XmlEncoding.Detect(bytes, sessionOptions.Charset);
            LogEncoding(detected);
            var text = XmlEncoding.Decode(bytes, detected);
            var parts = XmlIo.SplitDocuments(text);
            if (parts.Count > 1)
            {
                HelperLog.Reject($"Open requires a single well-formed document; use OpenMulti documents={parts.Count}");
                throw new XmlException($"Open requires a single well-formed document; use OpenMulti. documents={parts.Count}");
            }

            var (document, doctype) = XmlIo.LoadDocument(text, read);
            HelperLog.Information(
                app,
                VestigiumStatus.Success,
                HelperLog.Subcategories.Document,
                $"Open path={target} bytes={bytes.Length} encoding={detected.Name} winner={detected.Source} root={document.Root!.Name.LocalName} session={sessionId}");
            return new XmlSession(
                document,
                target,
                detected,
                XmlMediaType.Guess(null, looksLikeDtd: false),
                doctype,
                sessionOptions,
                sessionId);
        }
        catch (ArgumentException) { throw; }
        catch (XmlException) { throw; }
        catch (IOException) { throw; }
        catch (Exception ex)
        {
            HelperLog.Trap(ex);
            throw;
        }
    }

    public static XmlDocumentStream OpenMulti(string path, XmlSessionOptions? options = null)
    {
        const string app = HelperLog.AppIds.Xml;
        var target = Path.GetFullPath(HelperGuard.FileExists(path, nameof(path)));
        using var scope = HelperLog.Begin(app, HelperLog.Subcategories.Multi, "OpenMulti", "path=" + target);
        try
        {
            var bytes = XmlIo.ReadAllBytes(target);
            var sessionOptions = options ?? new XmlSessionOptions();
            var read = new XmlReadOptions
            {
                ProhibitDtd = sessionOptions.ProhibitDtd,
                Charset = sessionOptions.Charset,
                MaxCharacters = sessionOptions.MaxCharacters,
                MultiDocument = true
            };
            var detected = XmlEncoding.Detect(bytes, sessionOptions.Charset);
            LogEncoding(detected);
            var text = XmlEncoding.Decode(bytes, detected);
            var loaded = XmlIo.LoadMany(text, read);
            HelperLog.Information(
                app,
                VestigiumStatus.Success,
                HelperLog.Subcategories.Multi,
                $"OpenMulti path={target} documents={loaded.Count} bytes={bytes.Length}");
            var sessions = loaded.Select((item, i) =>
                new XmlSession(
                    item.Document,
                    target + "#" + (i + 1),
                    detected,
                    XmlMediaType.ApplicationXml,
                    item.Doctype,
                    sessionOptions,
                    HelperLog.NewId()));
            return new XmlDocumentStream(sessions, target);
        }
        catch (ArgumentException) { throw; }
        catch (XmlException) { throw; }
        catch (Exception ex)
        {
            HelperLog.Trap(ex);
            throw;
        }
    }

    public static XmlSession OpenExport(string stem, XmlSessionOptions? options = null)
    {
        var name = HelperGuard.NotBlank(stem, nameof(stem));
        var target = XmlIo.ResolveExportFile(DefaultExportDirectory(), name);
        return Open(target, options);
    }

    public static void WriteFile(string path, XDocument document, XmlWriteOptions? options = null)
    {
        const string app = HelperLog.AppIds.Xml;
        var target = Path.GetFullPath(HelperGuard.NotBlank(path, nameof(path)));
        using var scope = HelperLog.Begin(app, HelperLog.Subcategories.Save, "WriteFile", "path=" + target);
        try
        {
            var doc = HelperGuard.NotNull(document, nameof(document));
            var opts = options ?? new XmlWriteOptions();
            var bytes = XmlIo.Write(target, doc, doctype: null, opts);
            HelperLog.Information(
                app,
                VestigiumStatus.Success,
                HelperLog.Subcategories.Save,
                $"WriteFile path={target} bytes={bytes} collision={opts.Collision} atomic={opts.AtomicWrite}");
        }
        catch (ArgumentException) { throw; }
        catch (IOException) { throw; }
        catch (Exception ex)
        {
            HelperLog.Trap(ex);
            throw;
        }
    }

    private static XmlReadOptions ToReadOptions(XmlSessionOptions options)
        => new()
        {
            ProhibitDtd = options.ProhibitDtd,
            Charset = options.Charset,
            MaxCharacters = options.MaxCharacters
        };

    private static void GuardSize(long length, XmlReadOptions options)
    {
        if (length <= options.MaxCharacters)
            return;
        HelperLog.Reject($"document exceeds maxCharacters={options.MaxCharacters}");
        throw new XmlException($"Document exceeds maxCharacters={options.MaxCharacters}.");
    }

    private static void LogEncoding(XmlEncodingResult detected)
    {
        if (string.IsNullOrWhiteSpace(detected.Warning))
            return;
        HelperLog.Warning(
            HelperLog.AppIds.Xml,
            VestigiumStatus.Success,
            HelperLog.Subcategories.Document,
            detected.Warning);
    }
}
