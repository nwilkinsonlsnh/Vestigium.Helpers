using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;
using Vestigium.Helpers;
using Vestigium.Logging;

namespace Vestigium.Helpers.Xml;

internal static class XmlIo
{
    internal const int StreamBufferSize = 64 * 1024;
    private const string App = HelperLog.AppIds.Xml;

    private static readonly Regex CustomEntity = new(
        @"&(?!(?:lt|gt|amp|apos|quot|#\d+|#x[0-9A-Fa-f]+);)[A-Za-z_][\w.-]*;",
        RegexOptions.Compiled);

    internal static XmlReaderSettings SafeReaderSettings(XmlReadOptions options)
    {
        XmlTestHooks.ResolverAsks = 0;
        return new XmlReaderSettings
        {
            DtdProcessing = DtdProcessing.Prohibit,
            XmlResolver = null,
            IgnoreComments = false,
            IgnoreWhitespace = false,
            IgnoreProcessingInstructions = false,
            MaxCharactersInDocument = options.MaxCharacters > 0 ? options.MaxCharacters : 4_000_000,
            MaxCharactersFromEntities = 0,
            CloseInput = false,
            CheckCharacters = true,
            ConformanceLevel = ConformanceLevel.Document
        };
    }

    internal static (XDocument Document, string? Doctype) LoadDocument(string text, XmlReadOptions options)
    {
        var (doctype, remainder) = CaptureDoctype(text);
        remainder = NeutralizeUndeclaredEntities(remainder);
        if (doctype is not null)
        {
            HelperLog.Warning(
                App,
                VestigiumStatus.Success,
                HelperLog.Subcategories.Safety,
                "dtd ignored resolver=0");
        }

        using var reader = XmlReader.Create(new StringReader(remainder), SafeReaderSettings(options));
        var load = LoadOptions.PreserveWhitespace | LoadOptions.SetLineInfo;
        var document = XDocument.Load(reader, load);
        if (document.Root is null)
        {
            HelperLog.Reject("document has no root");
            throw new XmlException("XML document has no root.");
        }

        return (document, doctype);
    }

    internal static List<(XDocument Document, string? Doctype)> LoadMany(string text, XmlReadOptions options)
    {
        var parts = SplitDocuments(text);
        if (parts.Count == 0)
        {
            HelperLog.Reject("stream has no xml documents");
            throw new XmlException("XML stream has no documents.");
        }

        var list = new List<(XDocument, string?)>(parts.Count);
        var skipped = 0;
        foreach (var part in parts)
        {
            try
            {
                list.Add(LoadDocument(part, options));
            }
            catch (XmlException)
            {
                skipped++;
            }
        }
        if (skipped > 0)
        {
            HelperLog.Warning(
                App,
                VestigiumStatus.Success,
                HelperLog.Subcategories.Multi,
                $"skipped unparseable documents={skipped}");
        }
        if (list.Count == 0)
        {
            HelperLog.Reject("stream has no xml documents");
            throw new XmlException("XML stream has no documents.");
        }
        return list;
    }

    internal static List<string> SplitDocuments(string source)
    {
        var text = source.Length > 0 && source[0] == '\uFEFF' ? source[1..] : source;
        var docs = new List<string>();
        var offset = 0;
        while (offset < text.Length)
        {
            while (offset < text.Length && char.IsWhiteSpace(text[offset]))
                offset++;
            if (offset >= text.Length)
                break;
            var end = TakeOneDocument(text, offset);
            if (end is null)
                break;
            docs.Add(text[offset..end.Value]);
            offset = end.Value;
        }
        return docs;
    }

    internal static (string? Doctype, string Remainder) CaptureDoctype(string text)
    {
        var start = text.IndexOf("<!DOCTYPE", StringComparison.OrdinalIgnoreCase);
        if (start < 0)
            return (null, text);

        var i = start + 9;
        var depth = 0;
        char? quote = null;
        for (; i < text.Length; i++)
        {
            var c = text[i];
            if (quote is not null)
            {
                if (c == quote)
                    quote = null;
                continue;
            }
            if (c is '"' or '\'')
            {
                quote = c;
                continue;
            }
            if (c == '[')
                depth++;
            else if (c == ']')
                depth = Math.Max(0, depth - 1);
            else if (c == '>' && depth == 0)
            {
                i++;
                break;
            }
        }

        var doctype = text[start..i];
        var rest = text[..start] + text[i..];
        return (doctype, rest);
    }

    internal static string NeutralizeUndeclaredEntities(string text)
        => CustomEntity.Replace(text, string.Empty);

    internal static int Write(string path, XDocument document, string? doctype, XmlWriteOptions options)
    {
        RejectCollision(path, options.Collision, replaceInPlace: false);
        var xml = Serialize(document, doctype, options.Indent);
        var encoding = XmlEncoding.ForWrite(options.EmitBom);
        return WriteAtomic(path, options.AtomicWrite, dest =>
        {
            using var stream = new FileStream(dest, FileMode.Create, FileAccess.Write, FileShare.None, StreamBufferSize, FileOptions.SequentialScan);
            using (var writer = new StreamWriter(stream, encoding, StreamBufferSize, leaveOpen: true))
            {
                writer.Write(xml);
                writer.Flush();
            }
            stream.Flush(flushToDisk: true);
            return checked((int)stream.Length);
        });
    }

    internal static int WriteExisting(string path, XDocument document, string? doctype, XmlWriteOptions options, bool replaceInPlace)
    {
        RejectCollision(path, options.Collision, replaceInPlace);
        var xml = Serialize(document, doctype, options.Indent);
        var encoding = XmlEncoding.ForWrite(options.EmitBom);
        return WriteAtomic(path, options.AtomicWrite, dest =>
        {
            using var stream = new FileStream(dest, FileMode.Create, FileAccess.Write, FileShare.None, StreamBufferSize, FileOptions.SequentialScan);
            using (var writer = new StreamWriter(stream, encoding, StreamBufferSize, leaveOpen: true))
            {
                writer.Write(xml);
                writer.Flush();
            }
            stream.Flush(flushToDisk: true);
            return checked((int)stream.Length);
        });
    }

    internal static string Serialize(XDocument document, string? doctype, bool indent)
    {
        var declaration = document.Declaration ?? new XDeclaration("1.0", "utf-8", null);
        if (string.IsNullOrWhiteSpace(declaration.Encoding))
            declaration.Encoding = "utf-8";
        document.Declaration = declaration;

        var settings = new XmlWriterSettings
        {
            Indent = indent,
            OmitXmlDeclaration = false,
            Encoding = new UTF8Encoding(false),
            NewLineHandling = NewLineHandling.None
        };

        var builder = new StringBuilder();
        using (var writer = XmlWriter.Create(builder, settings))
        {
            document.Save(writer);
        }

        var xml = builder.ToString();
        if (doctype is not null)
        {
            var declEnd = xml.IndexOf("?>", StringComparison.Ordinal);
            if (declEnd >= 0)
                xml = xml[..(declEnd + 2)] + Environment.NewLine + doctype + xml[(declEnd + 2)..];
            else
                xml = doctype + Environment.NewLine + xml;
        }

        return xml;
    }

    internal static void RejectCollision(string path, XmlCollision collision, bool replaceInPlace)
    {
        if (replaceInPlace || collision == XmlCollision.Overwrite)
            return;
        if (!File.Exists(path))
            return;
        HelperLog.Reject($"dest exists path={path}");
        throw new IOException($"Destination already exists: {path}.");
    }

    internal static bool SamePath(string? left, string right)
    {
        if (string.IsNullOrWhiteSpace(left))
            return false;
        var a = Path.GetFullPath(left);
        var b = Path.GetFullPath(right);
        return OperatingSystem.IsWindows()
            ? string.Equals(a, b, StringComparison.OrdinalIgnoreCase)
            : string.Equals(a, b, StringComparison.Ordinal);
    }

    internal static string ResolveExportFile(string directory, string stem)
    {
        var file = stem.Trim();
        foreach (var ch in Path.GetInvalidFileNameChars())
            file = file.Replace(ch, '_');
        if (file is "." or ".." || string.IsNullOrWhiteSpace(file))
        {
            HelperLog.Reject("stem is not a file name");
            throw new ArgumentException("Export stem must be a file name.", nameof(stem));
        }

        if (!file.EndsWith(".xml", StringComparison.OrdinalIgnoreCase))
            file += ".xml";

        var dest = Path.GetFullPath(Path.Combine(directory, file));
        var root = Path.GetFullPath(directory);
        var prefix = root.EndsWith(Path.DirectorySeparatorChar) ? root : root + Path.DirectorySeparatorChar;
        if (!dest.StartsWith(prefix, StringComparison.Ordinal) && !string.Equals(dest, root, StringComparison.Ordinal))
        {
            HelperLog.Reject("export path escaped the export folder");
            throw new ArgumentException("Export stem must stay under the export folder.", nameof(stem));
        }

        return dest;
    }

    internal static byte[] ReadAllBytes(string path)
    {
        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, StreamBufferSize, FileOptions.SequentialScan);
        using var memory = new MemoryStream();
        stream.CopyTo(memory);
        return memory.ToArray();
    }

    private static int WriteAtomic(string path, bool atomic, Func<string, int> write)
    {
        var parent = Path.GetDirectoryName(path);
        if (!string.IsNullOrWhiteSpace(parent))
            Directory.CreateDirectory(parent);

        if (!atomic)
            return write(path);

        var temp = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            var bytes = write(temp);
            File.Move(temp, path, overwrite: true);
            return bytes;
        }
        catch
        {
            if (File.Exists(temp))
            {
                try { File.Delete(temp); }
                catch (IOException) { }
            }
            throw;
        }
    }

    private static int? TakeOneDocument(string text, int start)
    {
        var i = start;
        if (text.AsSpan(i).StartsWith("<?xml", StringComparison.OrdinalIgnoreCase))
        {
            var endDecl = text.IndexOf("?>", i, StringComparison.Ordinal);
            if (endDecl < 0)
                return null;
            i = endDecl + 2;
            while (i < text.Length && char.IsWhiteSpace(text[i]))
                i++;
        }
        if (text.AsSpan(i).StartsWith("<!DOCTYPE", StringComparison.OrdinalIgnoreCase))
        {
            var (_, remainder) = CaptureDoctype(text[i..]);
            i = text.Length - remainder.Length;
            while (i < text.Length && char.IsWhiteSpace(text[i]))
                i++;
        }
        if (i >= text.Length || text[i] != '<')
            return null;

        var nameMatch = Regex.Match(text[i..], @"^<\/?([A-Za-z_:][\w:.-]*)");
        if (!nameMatch.Success)
            return null;
        var rootName = nameMatch.Groups[1].Value;
        if (text[i + 1] == '/')
            return null;

        var depth = 0;
        char? quote = null;
        var inComment = false;
        var inCdata = false;
        var inPi = false;
        for (var p = i; p < text.Length; p++)
        {
            if (inComment)
            {
                if (p + 2 < text.Length && text[p] == '-' && text[p + 1] == '-' && text[p + 2] == '>')
                {
                    inComment = false;
                    p += 2;
                }
                continue;
            }
            if (inCdata)
            {
                if (p + 2 < text.Length && text[p] == ']' && text[p + 1] == ']' && text[p + 2] == '>')
                {
                    inCdata = false;
                    p += 2;
                }
                continue;
            }
            if (inPi)
            {
                if (p + 1 < text.Length && text[p] == '?' && text[p + 1] == '>')
                {
                    inPi = false;
                    p += 1;
                }
                continue;
            }
            if (quote is not null)
            {
                if (text[p] == quote)
                    quote = null;
                continue;
            }
            if (p + 3 < text.Length && text.AsSpan(p, 4).SequenceEqual("<!--"))
            {
                inComment = true;
                p += 3;
                continue;
            }
            if (p + 8 < text.Length && text.AsSpan(p, 9).SequenceEqual("<![CDATA["))
            {
                inCdata = true;
                p += 8;
                continue;
            }
            if (p + 1 < text.Length && text[p] == '<' && text[p + 1] == '?')
            {
                inPi = true;
                p += 1;
                continue;
            }

            var c = text[p];
            if (c is '"' or '\'')
            {
                quote = c;
                continue;
            }
            if (c != '<')
                continue;

            var rest = text.AsSpan(p);
            if (IsNamedOpen(rest, rootName, out var empty, out var tagLen))
            {
                if (!empty)
                {
                    if (depth >= 1 && p > start && p > 0 && text[p - 1] == '\n')
                        return p;
                    depth++;
                }
                else if (depth == 0)
                    return p + tagLen;
                p += tagLen - 1;
                continue;
            }
            if (IsNamedClose(rest, rootName, out var closeLen))
            {
                depth--;
                if (depth == 0)
                    return p + closeLen;
                p += closeLen - 1;
            }
        }
        return null;
    }

    private static bool IsNamedOpen(ReadOnlySpan<char> rest, string rootName, out bool empty, out int tagLen)
    {
        empty = false;
        tagLen = 0;
        if (rest.Length < rootName.Length + 1 || rest[0] != '<')
            return false;
        if (!rest[1..].StartsWith(rootName, StringComparison.Ordinal))
            return false;
        var after = 1 + rootName.Length;
        if (after >= rest.Length)
            return false;
        var next = rest[after];
        if (next is not (' ' or '/' or '>' or '\t' or '\r' or '\n'))
            return false;
        var end = rest.IndexOf('>');
        if (end < 0)
            return false;
        empty = end > 0 && rest[end - 1] == '/';
        tagLen = end + 1;
        return true;
    }

    private static bool IsNamedClose(ReadOnlySpan<char> rest, string rootName, out int tagLen)
    {
        tagLen = 0;
        var prefix = "</" + rootName;
        if (!rest.StartsWith(prefix, StringComparison.Ordinal))
            return false;
        var end = rest.IndexOf('>');
        if (end < 0)
            return false;
        tagLen = end + 1;
        return true;
    }
}
