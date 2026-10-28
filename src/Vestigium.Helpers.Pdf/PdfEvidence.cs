using System.Text;

namespace Vestigium.Helpers.Pdf;

/// <summary>Evidence PDFs. Build writes the report and the sealed trailer. Verify checks both.</summary>
public static class PdfEvidence
{
    /// <summary>Package version.</summary>
    public const string Version = "1.3.0";

    private const string BodyMark = "%%VESTIGIUM-BODY";

    /// <summary>Report pages, hashed, then the trailer page with the HMAC seal.</summary>
    public static byte[] Build(PdfDocument document)
    {
        var body = PdfPages.RenderBody(document);
        if (!document.Seal)
            return PdfPages.Render(document, "", "");
        var hash = PdfSeal.Body(body);
        var seal = PdfSeal.Seal(document.Identity, hash);
        var pdf = PdfPages.Render(document, hash, seal);
        var tail = Encoding.ASCII.GetBytes("\n" + BodyMark + "\n" + Convert.ToBase64String(body) + "\n");
        var all = new byte[pdf.Length + tail.Length];
        pdf.CopyTo(all, 0);
        tail.CopyTo(all, pdf.Length);
        return all;
    }

    public static byte[] Build(PdfReport report)
    {
        var body = PdfPages.RenderBody(report);
        var hash = PdfSeal.Body(body);
        var seal = PdfSeal.Seal(report.Identity, hash);
        var pdf = PdfPages.Render(report, hash, seal);
        var tail = Encoding.ASCII.GetBytes("\n" + BodyMark + "\n" + Convert.ToBase64String(body) + "\n");
        var all = new byte[pdf.Length + tail.Length];
        pdf.CopyTo(all, 0);
        tail.CopyTo(all, pdf.Length);
        return all;
    }

    /// <summary>Check a file. A missing file comes back as NoTrailer.</summary>
    public static PdfVerdict Verify(string path)
    {
        try
        {
            return Verify(File.ReadAllBytes(path));
        }
        catch (Exception)
        {
            return None();
        }
    }

    /// <summary>Check bytes. A file that is not ours comes back as NoTrailer.</summary>
    public static PdfVerdict Verify(ReadOnlySpan<byte> pdf)
    {
        try
        {
            var text = Encoding.Latin1.GetString(pdf);
            var bodyHash = Field(text, "body=");
            var seal = Field(text, "seal=");
            var canon = Field(text, "canon=");
            if (bodyHash is null || seal is null || canon is null)
                return None();

            var canonical = Encoding.UTF8.GetString(Convert.FromBase64String(canon));
            var carried = CarriedBody(text);
            var recomputed = carried is null ? null : PdfSeal.Body(carried);
            var bodyMatches = recomputed is not null && recomputed == bodyHash;

            var sealChecked = false;
            var sealMatches = false;
            try
            {
                sealChecked = true;
                sealMatches = PdfSeal.Seal(canonical, bodyHash) == seal;
            }
            catch (Exception)
            {
                sealChecked = false;
            }

            var valid = bodyMatches && sealChecked && sealMatches;
            return new PdfVerdict
            {
                Result = valid ? PdfVerifyResult.Valid : PdfVerifyResult.Tampered,
                BodyMatches = bodyMatches,
                SealChecked = sealChecked,
                SealMatches = sealMatches,
                BodyHash = bodyHash
            };
        }
        catch (Exception)
        {
            return None();
        }
    }

    private static PdfVerdict None() => new() { Result = PdfVerifyResult.NoTrailer };

    private static string? Field(string text, string name)
    {
        var at = text.IndexOf(name, StringComparison.Ordinal);
        if (at < 0)
            return null;
        var start = at + name.Length;
        var end = text.IndexOfAny(new[] { ';', ')', '\n', '\r' }, start);
        if (end < 0)
            end = text.Length;
        var value = text[start..end].Trim();
        return value.Length == 0 ? null : value;
    }

    private static byte[]? CarriedBody(string text)
    {
        var at = text.IndexOf(BodyMark, StringComparison.Ordinal);
        if (at < 0)
            return null;
        var start = text.IndexOf('\n', at);
        if (start < 0)
            return null;
        var raw = text[(start + 1)..].Trim();
        try
        {
            return Convert.FromBase64String(raw);
        }
        catch (FormatException)
        {
            return null;
        }
    }
}
