using System.Text.RegularExpressions;
using Vestigium.Helpers;

namespace Vestigium.Helpers.FileIo;

/// <summary>
/// UniqueName mint. Numeric <c>.##</c> → report.01.txt. Alpha <c>A##</c> → report.A01.txt then A02 … A99 then B01.
/// Width never shrinks. Cap returns null (NameCap) and never overwrites.
/// </summary>
public static class UniqueName
{
    public const string DefaultPattern = ".##";

    public readonly record struct Spec(bool Alpha, int Width);

    public static Spec Parse(string pattern)
    {
        var p = HelperGuard.NotBlank(pattern, nameof(pattern)).Trim();
        var num = Regex.Match(p, @"^\.(#{1,5})$");
        if (num.Success)
            return new Spec(false, num.Groups[1].Length);
        var alpha = Regex.Match(p, @"^A(#{1,5})$");
        if (alpha.Success)
            return new Spec(true, alpha.Groups[1].Length);
        throw new ArgumentException("UniqueNamePattern must be .#…##### or A#…A#####.", nameof(pattern));
    }

    public static string? Next(IReadOnlyList<string> existing, string originalName, string pattern)
    {
        ArgumentNullException.ThrowIfNull(existing);
        var name = HelperGuard.NotBlank(originalName, nameof(originalName));
        var spec = Parse(pattern);
        var dot = name.LastIndexOf('.');
        var stem = dot > 0 ? name[..dot] : name;
        var ext = dot > 0 ? name[dot..] : "";
        if (!spec.Alpha)
        {
            var re = new Regex("^" + Regex.Escape(stem) + @"\.(\d+)" + Regex.Escape(ext) + "$");
            var max = 0;
            foreach (var n in existing)
            {
                var m = re.Match(n);
                if (m.Success)
                    max = Math.Max(max, int.Parse(m.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture));
            }
            var cap = (int)Math.Pow(10, spec.Width) - 1;
            var next = max + 1;
            if (next > cap)
                return null;
            return $"{stem}.{next.ToString(System.Globalization.CultureInfo.InvariantCulture).PadLeft(spec.Width, '0')}{ext}";
        }

        var alphaRe = new Regex("^" + Regex.Escape(stem) + @"\.([A-Z])(\d{" + spec.Width + "})" + Regex.Escape(ext) + "$");
        var best = 0;
        var per = (int)Math.Pow(10, spec.Width) - 1;
        foreach (var n in existing)
        {
            var m = alphaRe.Match(n);
            if (!m.Success)
                continue;
            var letter = m.Groups[1].Value[0] - 64;
            var numVal = int.Parse(m.Groups[2].Value, System.Globalization.CultureInfo.InvariantCulture);
            best = Math.Max(best, (letter - 1) * per + numVal);
        }
        var nextAlpha = best + 1;
        if (nextAlpha > 26 * per)
            return null;
        var letterOut = (char)(65 + (nextAlpha - 1) / per);
        var numOut = ((nextAlpha - 1) % per) + 1;
        return $"{stem}.{letterOut}{numOut.ToString(System.Globalization.CultureInfo.InvariantCulture).PadLeft(spec.Width, '0')}{ext}";
    }
}
