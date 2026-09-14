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

    public static RegistryCompareSummary Compare(
        string leftIndex,
        string rightIndex,
        string output,
        bool confirm,
        bool force,
        bool includeSame,
        bool includePayload,
        IProgress<RegistryCompareProgress>? progress,
        CancellationToken cancel,
        IReadOnlyList<string>? ignorePathPrefixes = null)
    {
        try
        {
            return CompareCore(leftIndex, rightIndex, output, confirm, force, includeSame, includePayload, progress, cancel, ignorePathPrefixes);
        }
        catch (OperationCanceledException)
        {
            return new RegistryCompareSummary { Status = RegistryWriteStatus.Denied, Reason = "canceled", OutputPath = output, Verdict = "canceled" };
        }
    }

    private static RegistryCompareSummary CompareCore(
        string leftIndex,
        string rightIndex,
        string output,
        bool confirm,
        bool force,
        bool includeSame,
        bool includePayload,
        IProgress<RegistryCompareProgress>? progress,
        CancellationToken cancel,
        IReadOnlyList<string>? ignorePathPrefixes)
    {
        leftIndex = HelperGuard.FileExists(leftIndex, nameof(leftIndex));
        rightIndex = HelperGuard.FileExists(rightIndex, nameof(rightIndex));
        output = HelperGuard.NotBlank(output, nameof(output));
        if (!confirm)
            return new RegistryCompareSummary { Status = RegistryWriteStatus.Denied, Reason = "confirm=false", OutputPath = output };
        cancel.ThrowIfCancellationRequested();

        var left = LoadIndex(leftIndex);
        var right = LoadIndex(rightIndex);
        var ignored = DropIgnored(left, right, ignorePathPrefixes);
        progress?.Report(new RegistryCompareProgress { Phase = "Verify", KeysSeen = left.Keys.Count + right.Keys.Count });

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
            ["ignored"] = ignored,
            ["sampleLeftOnly"] = leftOnlyKeys.Take(12).ToArray(),
            ["sampleRightOnly"] = rightOnlyKeys.Take(12).ToArray()
        });

        var same = 0;
        var changed = 0;
        var leftOnly = 0;
        var rightOnly = 0;
        var deltas = new List<RegistryDelta>();
        if (!stop)
        {
            progress?.Report(new RegistryCompareProgress { Phase = "Merge", KeysSeen = union });
            Merge(left, right, writer, includeSame, includePayload, deltas, ref same, ref changed, ref leftOnly, ref rightOnly, cancel);
        }

        Write(writer, new Dictionary<string, object?>
        {
            ["rec"] = "footer",
            ["same"] = same,
            ["changed"] = changed,
            ["leftOnly"] = leftOnly,
            ["rightOnly"] = rightOnly,
            ["ignored"] = ignored,
            ["stopped"] = stop
        });

        HelperLog.Information(HelperLog.AppIds.WinReg, VestigiumStatus.Success, HelperLog.Subcategories.Inventory, $"Compare verdict={verdict} changed={changed} ignored={ignored}");
        progress?.Report(new RegistryCompareProgress { Phase = "Done", KeysSeen = union, Same = same, Changed = changed, LeftOnly = leftOnly, RightOnly = rightOnly });
        return new RegistryCompareSummary
        {
            Status = RegistryWriteStatus.Ok,
            Reason = verdict,
            Verdict = verdict,
            Relatedness = relatedness,
            Delta = 1d - relatedness,
            Stopped = stop,
            Same = same,
            Changed = changed,
            LeftOnly = leftOnly,
            RightOnly = rightOnly,
            OutputPath = output,
            Deltas = deltas.Take(RegistryCompareSummary.MaxDeltas).ToList()
        };
    }

    internal static int DropIgnored(IndexFile left, IndexFile right, IReadOnlyList<string>? prefixes)
    {
        if (prefixes is null || prefixes.Count == 0)
            return 0;
        var list = prefixes.Where(p => !string.IsNullOrWhiteSpace(p)).ToArray();
        if (list.Length == 0)
            return 0;
        return Drop(left, list) + Drop(right, list);
    }

    private static int Drop(IndexFile file, string[] prefixes)
    {
        var dropped = 0;
        foreach (var key in file.Keys.ToList())
        {
            if (!IsIgnored(key, prefixes))
                continue;
            file.Keys.Remove(key);
            dropped++;
        }

        foreach (var id in file.Values.Keys.ToList())
        {
            if (!IsIgnored(file.Values[id].Path, prefixes))
                continue;
            file.Values.Remove(id);
            dropped++;
        }

        return dropped;
    }

    internal static bool IsIgnored(string path, IReadOnlyList<string> prefixes)
    {
        foreach (var prefix in prefixes)
        {
            if (string.IsNullOrWhiteSpace(prefix))
                continue;
            if (path.Equals(prefix, StringComparison.OrdinalIgnoreCase))
                return true;
            if (path.StartsWith(prefix + "\\", StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }

    private static void Merge(
        IndexFile left,
        IndexFile right,
        StreamWriter writer,
        bool includeSame,
        bool includePayload,
        List<RegistryDelta> deltas,
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
                Add(writer, deltas, "RightOnly", r!, null, includePayload: false);
                continue;
            }
            if (r is null)
            {
                leftOnly++;
                Add(writer, deltas, "LeftOnly", l, null, includePayload: false);
                continue;
            }

            if (string.Equals(l.Type, r.Type, StringComparison.OrdinalIgnoreCase)
                && string.Equals(l.Hash, r.Hash, StringComparison.OrdinalIgnoreCase))
            {
                same++;
                if (includeSame)
                    Add(writer, deltas, "Same", l, r, includePayload: false);
                continue;
            }

            changed++;
            Add(writer, deltas, "Changed", l, r, includePayload);
        }
    }

    private static void Add(StreamWriter writer, List<RegistryDelta> deltas, string kind, IndexValue left, IndexValue? right, bool includePayload)
    {
        var useRight = kind == "RightOnly" && right is not null;
        var path = useRight ? right!.Path : left.Path;
        var name = useRight ? right!.Name : left.Name;
        var leftText = includePayload ? Small(left.Text) : null;
        var rightText = includePayload ? Small(right?.Text) : null;
        var delta = new RegistryDelta
        {
            Kind = kind,
            Path = path,
            Name = name,
            LeftType = useRight ? null : left.Type,
            RightType = right?.Type,
            LeftHash = useRight ? null : left.Hash,
            RightHash = right?.Hash,
            LeftText = leftText,
            RightText = rightText
        };
        deltas.Add(delta);
        var row = new Dictionary<string, object?>
        {
            ["rec"] = "delta",
            ["kind"] = kind,
            ["path"] = path,
            ["name"] = name,
            ["leftType"] = delta.LeftType,
            ["rightType"] = delta.RightType,
            ["leftHash"] = delta.LeftHash,
            ["rightHash"] = delta.RightHash
        };
        if (includePayload)
        {
            row["leftText"] = leftText;
            row["rightText"] = rightText;
        }
        Write(writer, row);
    }

    private static string? Small(string? text)
        => text is { Length: > 0 and <= 256 } ? text : null;

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
                var item = new IndexValue(Str(root, "path"), Str(root, "name"), Str(root, "type"), Str(root, "hash"), Str(root, "text"));
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

    internal sealed record IndexValue(string Path, string Name, string Type, string Hash, string Text)
    {
        public string Id => Path + "\0" + Name;
    }
}
