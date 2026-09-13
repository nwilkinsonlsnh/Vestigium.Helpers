using System.Text;
using System.Text.Json;
using Vestigium.Helpers;
using Vestigium.Logging;

namespace Vestigium.Helpers.WinReg;

internal static class RegistryComparer
{
    public const string CompareSchema = "vest-regcmp/1";
    public const double RelatedFloor = 0.70;
    public const double UnrelatedCeiling = 0.30;
    public const double SubsetCoverage = 0.90;

    public static RegistryWriteResult Compare(
        string leftIndex,
        string rightIndex,
        string output,
        bool confirm,
        bool force,
        bool includeSame,
        bool includePayload,
        IProgress<RegistryCompareProgress>? progress,
        CancellationToken cancel)
    {
        _ = includePayload;
        leftIndex = HelperGuard.FileExists(leftIndex, nameof(leftIndex));
        rightIndex = HelperGuard.FileExists(rightIndex, nameof(rightIndex));
        output = HelperGuard.NotBlank(output, nameof(output));
        if (!confirm)
            return new RegistryWriteResult(RegistryWriteStatus.Denied, RegistryHiveKind.CurrentUser, output, null, "confirm=false");

        var left = LoadIndex(leftIndex);
        var right = LoadIndex(rightIndex);
        progress?.Report(new RegistryCompareProgress { Phase = "Verify", KeysSeen = left.Keys.Count + right.Keys.Count });
        cancel.ThrowIfCancellationRequested();

        var headerMatch = string.Equals(left.Hive, right.Hive, StringComparison.OrdinalIgnoreCase)
            && string.Equals(left.Path, right.Path, StringComparison.OrdinalIgnoreCase)
            && string.Equals(left.View, right.View, StringComparison.OrdinalIgnoreCase);

        var shared = left.Keys.Intersect(right.Keys, StringComparer.OrdinalIgnoreCase).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var leftOnlyKeys = left.Keys.Except(right.Keys, StringComparer.OrdinalIgnoreCase).ToList();
        var rightOnlyKeys = right.Keys.Except(left.Keys, StringComparer.OrdinalIgnoreCase).ToList();
        var union = left.Keys.Count + right.Keys.Count - shared.Count;
        var relatedness = union == 0 ? 0d : (double)shared.Count / union;
        var coverageLeft = left.Keys.Count == 0 ? 0d : (double)shared.Count / left.Keys.Count;
        var coverageRight = right.Keys.Count == 0 ? 0d : (double)shared.Count / right.Keys.Count;
        var verdict = Verdict(headerMatch, relatedness, coverageLeft, coverageRight);
        var stop = verdict == "Unrelated" && !force;

        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(output)) is { Length: > 0 } dir ? dir : ".");
        using var writer = new StreamWriter(output, false, new UTF8Encoding(false));
        Write(writer, new Dictionary<string, object?>
        {
            ["rec"] = "header",
            ["schema"] = CompareSchema,
            ["left"] = Side(left),
            ["right"] = Side(right)
        });
        Write(writer, new Dictionary<string, object?>
        {
            ["rec"] = "verify",
            ["verdict"] = verdict,
            ["relatedness"] = relatedness,
            ["delta"] = 1d - relatedness,
            ["coverageLeft"] = coverageLeft,
            ["coverageRight"] = coverageRight,
            ["keysLeft"] = left.Keys.Count,
            ["keysRight"] = right.Keys.Count,
            ["keysShared"] = shared.Count,
            ["sampleLeftOnly"] = leftOnlyKeys.Take(12).ToArray(),
            ["sampleRightOnly"] = rightOnlyKeys.Take(12).ToArray()
        });

        var same = 0;
        var changed = 0;
        var leftOnly = 0;
        var rightOnly = 0;
        if (!stop)
        {
            progress?.Report(new RegistryCompareProgress { Phase = "Merge", KeysSeen = union });
            Merge(left, right, writer, includeSame, ref same, ref changed, ref leftOnly, ref rightOnly, cancel);
        }

        Write(writer, new Dictionary<string, object?>
        {
            ["rec"] = "footer",
            ["same"] = same,
            ["changed"] = changed,
            ["leftOnly"] = leftOnly,
            ["rightOnly"] = rightOnly,
            ["stopped"] = stop
        });

        HelperLog.Information(HelperLog.AppIds.WinReg, VestigiumStatus.Success, HelperLog.Subcategories.Inventory, $"Compare verdict={verdict} changed={changed} left={leftOnly} right={rightOnly}");
        progress?.Report(new RegistryCompareProgress { Phase = "Done", KeysSeen = union, Same = same, Changed = changed, LeftOnly = leftOnly, RightOnly = rightOnly });
        return new RegistryWriteResult(RegistryWriteStatus.Ok, RegistryHiveKind.CurrentUser, output, null, verdict);
    }

    private static void Merge(
        IndexFile left,
        IndexFile right,
        StreamWriter writer,
        bool includeSame,
        ref int same,
        ref int changed,
        ref int leftOnly,
        ref int rightOnly,
        CancellationToken cancel)
    {
        foreach (var id in left.Values.Keys.Union(right.Values.Keys, StringComparer.OrdinalIgnoreCase).OrderBy(k => k, StringComparer.OrdinalIgnoreCase))
        {
            cancel.ThrowIfCancellationRequested();
            left.Values.TryGetValue(id, out var l);
            right.Values.TryGetValue(id, out var r);
            if (l is null)
            {
                rightOnly++;
                WriteDelta(writer, "RightOnly", r!);
                continue;
            }
            if (r is null)
            {
                leftOnly++;
                WriteDelta(writer, "LeftOnly", l);
                continue;
            }

            if (string.Equals(l.Type, r.Type, StringComparison.OrdinalIgnoreCase)
                && string.Equals(l.Hash, r.Hash, StringComparison.OrdinalIgnoreCase))
            {
                same++;
                if (includeSame)
                    WriteDelta(writer, "Same", l, r);
                continue;
            }

            changed++;
            WriteDelta(writer, "Changed", l, r);
        }
    }

    private static void WriteDelta(StreamWriter writer, string kind, IndexValue left, IndexValue? right = null)
    {
        var row = new Dictionary<string, object?>
        {
            ["rec"] = "delta",
            ["kind"] = kind,
            ["path"] = left.Path,
            ["name"] = left.Name,
            ["leftType"] = left.Type,
            ["rightType"] = right?.Type,
            ["leftHash"] = left.Hash,
            ["rightHash"] = right?.Hash
        };
        if (kind == "RightOnly" && right is not null)
        {
            row["path"] = right.Path;
            row["name"] = right.Name;
            row["leftType"] = null;
            row["leftHash"] = null;
            row["rightType"] = right.Type;
            row["rightHash"] = right.Hash;
        }
        Write(writer, row);
    }

    internal static string Verdict(bool headerMatch, double relatedness, double coverageLeft, double coverageRight)
    {
        if (!headerMatch)
            return "Unrelated";
        if (relatedness >= RelatedFloor)
            return "Related";
        if (coverageLeft >= SubsetCoverage && coverageLeft >= coverageRight)
            return "SubsetRight";
        if (coverageRight >= SubsetCoverage)
            return "SubsetLeft";
        if (relatedness < UnrelatedCeiling)
            return "Unrelated";
        return "Weak";
    }

    internal static IndexFile LoadIndex(string path)
    {
        var file = new IndexFile();
        foreach (var line in File.ReadLines(path))
        {
            if (string.IsNullOrWhiteSpace(line))
                continue;
            using var doc = JsonDocument.Parse(line);
            var root = doc.RootElement;
            if (!root.TryGetProperty("rec", out var rec))
                continue;
            var kind = rec.GetString();
            if (kind == "header")
            {
                if (root.TryGetProperty("schema", out var schema) && schema.GetString() != RegistryIndexWriter.Schema)
                    throw new ArgumentException("Not a vest-regidx/1 index.", nameof(path));
                file.Machine = Str(root, "machine");
                file.Hive = Str(root, "hive");
                file.Path = Str(root, "path");
                file.View = Str(root, "view");
                file.CapturedAt = Str(root, "capturedAt");
                file.Source = Str(root, "source");
            }
            else if (kind == "key")
            {
                file.Keys.Add(Str(root, "path"));
            }
            else if (kind == "value")
            {
                var item = new IndexValue(Str(root, "path"), Str(root, "name"), Str(root, "type"), Str(root, "hash"));
                file.Values[item.Id] = item;
            }
        }
        return file;
    }

    private static Dictionary<string, object?> Side(IndexFile file) => new()
    {
        ["machine"] = file.Machine,
        ["capturedAt"] = file.CapturedAt,
        ["hive"] = file.Hive,
        ["path"] = file.Path,
        ["view"] = file.View,
        ["source"] = file.Source
    };

    private static string Str(JsonElement root, string name)
        => root.TryGetProperty(name, out var el) ? el.GetString() ?? string.Empty : string.Empty;

    private static void Write(StreamWriter writer, Dictionary<string, object?> map)
        => writer.WriteLine(JsonSerializer.Serialize(map));

    internal sealed class IndexFile
    {
        public string Machine { get; set; } = "";
        public string Hive { get; set; } = "";
        public string Path { get; set; } = "";
        public string View { get; set; } = "";
        public string CapturedAt { get; set; } = "";
        public string Source { get; set; } = "";
        public HashSet<string> Keys { get; } = new(StringComparer.OrdinalIgnoreCase);
        public Dictionary<string, IndexValue> Values { get; } = new(StringComparer.OrdinalIgnoreCase);
    }

    internal sealed record IndexValue(string Path, string Name, string Type, string Hash)
    {
        public string Id => Path + "\0" + Name;
    }
}
