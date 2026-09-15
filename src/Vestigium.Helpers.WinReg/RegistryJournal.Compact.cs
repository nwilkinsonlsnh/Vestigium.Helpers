using System.Text;
using System.Text.Json;
using Vestigium.Helpers;
using Vestigium.Logging;

namespace Vestigium.Helpers.WinReg;

public sealed partial class RegistryJournal
{
    public static RegistryPurgeResult Compact(string path, RegistryPurgeOptions options)
    {
        path = HelperGuard.NotBlank(path, nameof(path));
        if (!options.DryRun && !options.Confirm)
            return Fail(path, RegistryWriteStatus.Denied, "confirm=false", options.DryRun);
        if (!File.Exists(path))
            return Fail(path, RegistryWriteStatus.NotFound, "journal missing", options.DryRun);

        var bytesBefore = new FileInfo(path).Length;
        var lines = File.ReadAllLines(path);
        if (lines.Length == 0)
            return Fail(path, RegistryWriteStatus.InvalidPath, "empty journal", options.DryRun);

        var header = lines[0];
        var batches = ParseBatches(lines);
        var keep = Decide(batches, options);
        var kept = batches.Where(b => keep.Contains(b.Id)).ToList();
        var removed = batches.Count - kept.Count;
        var mutsKept = kept.Sum(b => b.MutLines.Count);

        if (options.DryRun)
        {
            return new RegistryPurgeResult
            {
                Status = RegistryWriteStatus.Ok,
                Path = path,
                BatchesKept = kept.Count,
                BatchesRemoved = removed,
                MutsKept = mutsKept,
                BytesBefore = bytesBefore,
                BytesAfter = bytesBefore,
                DryRun = true
            };
        }

        if (kept.Count == 0)
        {
            File.Delete(path);
            return new RegistryPurgeResult
            {
                Status = RegistryWriteStatus.Ok,
                Path = path,
                BatchesRemoved = removed,
                BytesBefore = bytesBefore,
                Reason = "deleted"
            };
        }

        var tmp = path + ".tmp";
        using (var writer = new StreamWriter(tmp, false, new UTF8Encoding(false)))
        {
            writer.WriteLine(header);
            foreach (var batch in kept)
            {
                foreach (var line in batch.AllLines)
                    writer.WriteLine(line);
            }
        }

        File.Copy(tmp, path, overwrite: true);
        File.Delete(tmp);
        var bytesAfter = new FileInfo(path).Length;
        HelperLog.Information(HelperLog.AppIds.WinReg, VestigiumStatus.Success, HelperLog.Subcategories.Inventory,
            $"Purge path={path} kept={kept.Count} removed={removed} muts={mutsKept}");
        return new RegistryPurgeResult
        {
            Status = RegistryWriteStatus.Ok,
            Path = path,
            BatchesKept = kept.Count,
            BatchesRemoved = removed,
            MutsKept = mutsKept,
            BytesBefore = bytesBefore,
            BytesAfter = bytesAfter
        };
    }

    private static HashSet<string> Decide(List<BatchBlock> batches, RegistryPurgeOptions options)
    {
        var committed = batches.Where(b => b.Status == "committed").ToList();
        var keep = new HashSet<string>(StringComparer.Ordinal);
        foreach (var batch in batches.Where(b => b.Status != "committed"))
            keep.Add(batch.Id);

        IEnumerable<BatchBlock> survivors = committed;
        if (!string.IsNullOrWhiteSpace(options.BatchId))
            survivors = committed.Where(b => !b.Id.Equals(options.BatchId, StringComparison.OrdinalIgnoreCase));
        else
        {
            if (options.KeepLastBatches is > 0 and var n)
                survivors = survivors.TakeLast(n);
            if (options.OlderThan is { } age)
            {
                var cutoff = DateTime.UtcNow - age;
                survivors = survivors.Where(b => b.StartedAt is null || b.StartedAt >= cutoff);
            }
            if (options.UndoneOnly)
                survivors = survivors.Where(b => !b.FullyUndone);
        }

        foreach (var batch in survivors)
            keep.Add(batch.Id);
        return keep;
    }

    private static List<BatchBlock> ParseBatches(string[] lines)
    {
        var batches = new List<BatchBlock>();
        BatchBlock? current = null;
        var undos = new List<(string Batch, string Line)>();
        foreach (var line in lines.Skip(1))
        {
            if (string.IsNullOrWhiteSpace(line))
                continue;
            using var doc = JsonDocument.Parse(line);
            var rec = doc.RootElement.GetProperty("rec").GetString();
            switch (rec)
            {
                case "batch":
                    current = new BatchBlock
                    {
                        Id = doc.RootElement.GetProperty("id").GetString() ?? "",
                        StartedAt = doc.RootElement.TryGetProperty("startedAt", out var at) && DateTime.TryParse(at.GetString(), out var parsed) ? parsed.ToUniversalTime() : null
                    };
                    current.AllLines.Add(line);
                    batches.Add(current);
                    break;
                case "mut":
                    if (doc.RootElement.TryGetProperty("op", out var op) && op.GetString() == "mut-undo")
                    {
                        var target = doc.RootElement.TryGetProperty("targetBatch", out var tb) ? tb.GetString() ?? "" : "";
                        undos.Add((target, line));
                        break;
                    }
                    current?.AllLines.Add(line);
                    current?.MutLines.Add(line);
                    break;
                case "batch-end":
                    if (current is not null)
                    {
                        current.AllLines.Add(line);
                        current.Status = doc.RootElement.TryGetProperty("status", out var st) ? st.GetString() ?? "committed" : "committed";
                    }
                    current = null;
                    break;
            }
        }

        foreach (var (batchId, line) in undos)
        {
            var batch = batches.FirstOrDefault(b => b.Id == batchId);
            if (batch is null)
                continue;
            batch.UndoLines.Add(line);
            batch.AllLines.Add(line);
        }

        foreach (var batch in batches)
            batch.FullyUndone = batch.MutLines.Count > 0 && batch.UndoLines.Count >= batch.MutLines.Count;

        return batches;
    }

    private static RegistryPurgeResult Fail(string path, RegistryWriteStatus status, string reason, bool dryRun)
        => new() { Status = status, Path = path, Reason = reason, DryRun = dryRun };

    private sealed class BatchBlock
    {
        public string Id { get; set; } = "";
        public string Status { get; set; } = "open";
        public DateTime? StartedAt { get; set; }
        public bool FullyUndone { get; set; }
        public List<string> MutLines { get; } = [];
        public List<string> UndoLines { get; } = [];
        public List<string> AllLines { get; } = [];
    }
}
