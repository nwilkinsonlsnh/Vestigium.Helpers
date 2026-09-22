using System.Globalization;

namespace Vestigium.Helpers.ClosedXml;

/// <summary>
/// Approximate Office Table Design chips (header / band / band-alt).
/// Excel still applies the real built-in <c>TableStyle*</c> theme; this is the gallery preview.
/// </summary>
public sealed class ExcelTableStylePreview
{
    public const string White = "#FFFFFF";
    public const string Ink = "#1F1F1F";
    public const string Black = "#1A1A1A";

    public required string Id { get; init; }
    public required string Group { get; init; }
    public required int Index { get; init; }
    public required string ExcelName { get; init; }
    public required string Header { get; init; }
    public required string HeaderInk { get; init; }
    public required string Band { get; init; }
    public required string BandAlt { get; init; }
    public required string BodyInk { get; init; }

    public string Caption => $"{Group} {Index}";

    private static readonly (string Fill, string Dark, string Pale, string Ink)[] Accents =
    [
        ("#5B9BD5", "#2F5496", "#DDEBF7", White),
        ("#ED7D31", "#C45911", "#FCE4D6", White),
        ("#A5A5A5", "#7F7F7F", "#F2F2F2", White),
        ("#FFC000", "#BF8F00", "#FFF2CC", Ink),
        ("#4472C4", "#203864", "#D6DCE4", White),
        ("#70AD47", "#375623", "#E2EFDA", White),
        ("#9C5700", "#833C0C", "#F8CBAD", White)
    ];

    public static IReadOnlyList<ExcelTableStylePreview> All { get; } = Build();

    public static IReadOnlyList<ExcelTableStylePreview> In(string group) =>
        All.Where(p => p.Group.Equals(group, StringComparison.OrdinalIgnoreCase)).ToArray();

    public static ExcelTableStylePreview Of(string? id)
    {
        var key = ExcelTableStyles.Normalize(id);
        var found = All.FirstOrDefault(p => p.Id.Equals(key, StringComparison.OrdinalIgnoreCase));
        return found ?? throw new ArgumentOutOfRangeException(nameof(id), $"No preview for '{id}'.");
    }

    private static ExcelTableStylePreview[] Build()
    {
        var list = new List<ExcelTableStylePreview>(60);
        AddGroup(list, "Light", 21);
        AddGroup(list, "Medium", 28);
        AddGroup(list, "Dark", 11);
        return [.. list];
    }

    private static void AddGroup(List<ExcelTableStylePreview> list, string group, int count)
    {
        for (var index = 1; index <= count; index++)
        {
            var id = group + index.ToString(CultureInfo.InvariantCulture);
            var colors = Colors(group, index);
            list.Add(new ExcelTableStylePreview
            {
                Id = id,
                Group = group,
                Index = index,
                ExcelName = "TableStyle" + id,
                Header = colors.Header,
                HeaderInk = colors.HeaderInk,
                Band = colors.Band,
                BandAlt = colors.BandAlt,
                BodyInk = group == "Dark" ? White : Ink
            });
        }
    }

    private static (string Header, string HeaderInk, string Band, string BandAlt) Colors(string group, int index)
    {
        var a = Accents[(index - 1) % Accents.Length];
        if (group == "Light")
        {
            if (index <= 7)
                return (White, a.Dark, a.Pale, White);
            if (index <= 14)
                return (a.Pale, a.Dark, Mix(a.Pale, White, 0.45), White);
            return (White, a.Dark, Mix(a.Pale, White, 0.2), White);
        }

        if (group == "Medium")
        {
            if (index <= 7)
                return (a.Fill, a.Ink, a.Pale, White);
            if (index <= 14)
                return (a.Dark, White, a.Pale, White);
            if (index <= 21)
                return (Mix(a.Dark, Black, 0.25), White, Mix(a.Fill, White, 0.55), Mix(a.Pale, White, 0.2));
            return (Black, White, a.Pale, Mix(a.Fill, White, 0.72));
        }

        if (index <= 7)
            return (Mix(a.Dark, Black, 0.35), White, a.Fill, a.Dark);
        return (Black, White, a.Dark, Mix(a.Dark, Black, 0.2));
    }

    private static string Mix(string a, string b, double t)
    {
        var pa = Rgb(a);
        var pb = Rgb(b);
        byte M(byte x, byte y) => (byte)Math.Clamp((int)Math.Round(x + (y - x) * t), 0, 255);
        return $"#{M(pa.R, pb.R):X2}{M(pa.G, pb.G):X2}{M(pa.B, pb.B):X2}";
    }

    private static (byte R, byte G, byte B) Rgb(string hex)
    {
        var h = hex.StartsWith('#') ? hex[1..] : hex;
        return (
            byte.Parse(h[..2], NumberStyles.HexNumber, CultureInfo.InvariantCulture),
            byte.Parse(h[2..4], NumberStyles.HexNumber, CultureInfo.InvariantCulture),
            byte.Parse(h[4..6], NumberStyles.HexNumber, CultureInfo.InvariantCulture));
    }
}
