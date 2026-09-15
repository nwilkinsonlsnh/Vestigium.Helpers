using System.Text;
using System.Text.Json;
using Vestigium.Helpers;
using Vestigium.Logging;

namespace Vestigium.Helpers.WinReg;

public sealed partial class RegistryJournal : IDisposable
{
    public const string Schema = "vest-regjnl/1";
    public const int MaxMutations = 100_000;

    private readonly StreamWriter _writer;
    private string? _openBatch;
    private int _seq;
    private int _total;
    private bool _disposed;

    private RegistryJournal(string path, StreamWriter writer, bool protect)
    {
        Path = path;
        Protect = protect;
        _writer = writer;
    }

    public string Path { get; }
    public bool Protect { get; }
    public string? OpenBatchId => _openBatch;
    public int MutationCount => _total;

    public static RegistryJournal? Create(
        string path,
        bool confirm,
        bool protect = false,
        out RegistryWriteResult result)
    {
        path = HelperGuard.NotBlank(path, nameof(path));
        if (!confirm)
        {
            result = Denied(path, "confirm=false");
            return null;
        }

        if (File.Exists(path))
        {
            result = new RegistryWriteResult(RegistryWriteStatus.InUse, RegistryHiveKind.CurrentUser, path, null, "journal exists");
            return null;
        }

        Directory.CreateDirectory(System.IO.Path.GetDirectoryName(System.IO.Path.GetFullPath(path)) is { Length: > 0 } dir ? dir : ".");
        var writer = new StreamWriter(path, false, new UTF8Encoding(false));
        var journal = new RegistryJournal(path, writer, protect);
        journal.Write(new Dictionary<string, object?>
        {
            ["rec"] = "header",
            ["schema"] = Schema,
            ["machine"] = Environment.MachineName,
            ["createdAt"] = DateTime.UtcNow.ToString("o"),
            ["protect"] = protect
        });
        HelperLog.Information(HelperLog.AppIds.WinReg, VestigiumStatus.Success, HelperLog.Subcategories.Inventory, $"Journal create path={path} protect={protect}");
        result = new RegistryWriteResult(RegistryWriteStatus.Ok, RegistryHiveKind.CurrentUser, path, null, null);
        return journal;
    }

    public static RegistryJournal? Load(string path, bool confirm, out RegistryWriteResult result)
    {
        path = HelperGuard.NotBlank(path, nameof(path));
        if (!confirm)
        {
            result = Denied(path, "confirm=false");
            return null;
        }

        if (!File.Exists(path))
        {
            result = new RegistryWriteResult(RegistryWriteStatus.NotFound, RegistryHiveKind.CurrentUser, path, null, "journal missing");
            return null;
        }

        var info = ReadInfo(path);
        if (info.Schema != Schema)
        {
            result = new RegistryWriteResult(RegistryWriteStatus.InvalidPath, RegistryHiveKind.CurrentUser, path, null, "schema " + info.Schema);
            return null;
        }

        var writer = new StreamWriter(path, true, new UTF8Encoding(false));
        var journal = new RegistryJournal(path, writer, info.Protect);
        journal._total = info.Batches.Sum(b => b.Mutations);
        result = new RegistryWriteResult(RegistryWriteStatus.Ok, RegistryHiveKind.CurrentUser, path, null, null);
        return journal;
    }

    public static RegistryJournalInfo ReadInfo(string path)
    {
        path = HelperGuard.NotBlank(path, nameof(path));
        var batches = new List<RegistryJournalBatch>();
        var schema = "";
        var machine = (string?)null;
        var protect = false;
        var openKind = "";
        var openLabel = (string?)null;
        var openId = "";
        var openCount = 0;

        foreach (var line in File.ReadLines(path))
        {
            if (string.IsNullOrWhiteSpace(line))
                continue;
            using var doc = JsonDocument.Parse(line);
            var rec = doc.RootElement.GetProperty("rec").GetString();
            switch (rec)
            {
                case "header":
                    schema = doc.RootElement.GetProperty("schema").GetString() ?? "";
                    machine = doc.RootElement.TryGetProperty("machine", out var m) ? m.GetString() : null;
                    protect = doc.RootElement.TryGetProperty("protect", out var p) && p.ValueKind == JsonValueKind.True;
                    break;
                case "batch":
                    openId = doc.RootElement.GetProperty("id").GetString() ?? "";
                    openKind = doc.RootElement.GetProperty("kind").GetString() ?? "";
                    openLabel = doc.RootElement.TryGetProperty("label", out var l) ? l.GetString() : null;
                    openCount = 0;
                    break;
                case "mut":
                    openCount++;
                    break;
                case "batch-end":
                    batches.Add(new RegistryJournalBatch
                    {
                        Id = doc.RootElement.GetProperty("id").GetString() ?? openId,
                        Kind = openKind,
                        Label = openLabel,
                        Status = doc.RootElement.TryGetProperty("status", out var s) ? s.GetString() ?? "committed" : "committed",
                        Mutations = doc.RootElement.TryGetProperty("muts", out var n) ? n.GetInt32() : openCount
                    });
                    openId = "";
                    break;
            }
        }

        if (openId.Length > 0)
        {
            batches.Add(new RegistryJournalBatch
            {
                Id = openId,
                Kind = openKind,
                Label = openLabel,
                Status = "open",
                Mutations = openCount
            });
        }

        return new RegistryJournalInfo
        {
            Path = path,
            Schema = schema,
            Machine = machine,
            Protect = protect,
            Batches = batches
        };
    }

    public RegistryWriteResult BeginBatch(string kind, string? label = null)
    {
        if (_openBatch is not null)
            return Denied(Path, "batch already open");
        var id = Guid.NewGuid().ToString("N")[..12];
        _openBatch = id;
        _seq = 0;
        Write(new Dictionary<string, object?>
        {
            ["rec"] = "batch",
            ["id"] = id,
            ["kind"] = kind,
            ["label"] = label,
            ["startedAt"] = DateTime.UtcNow.ToString("o")
        });
        return new RegistryWriteResult(RegistryWriteStatus.Ok, RegistryHiveKind.CurrentUser, Path, null, id);
    }

    public RegistryWriteResult CommitBatch(string status = "committed")
    {
        if (_openBatch is null)
            return Denied(Path, "no open batch");
        Write(new Dictionary<string, object?>
        {
            ["rec"] = "batch-end",
            ["id"] = _openBatch,
            ["status"] = status,
            ["muts"] = _seq
        });
        _openBatch = null;
        return new RegistryWriteResult(RegistryWriteStatus.Ok, RegistryHiveKind.CurrentUser, Path, null, status);
    }

    internal RegistryWriteResult AppendMut(Dictionary<string, object?> row)
    {
        if (_openBatch is null)
            return Denied(Path, "no open batch");
        if (_total >= MaxMutations)
            return Denied(Path, "MaxMutations cap is " + MaxMutations);
        _seq++;
        _total++;
        row["rec"] = "mut";
        row["batch"] = _openBatch;
        row["seq"] = _seq;
        Write(row);
        return new RegistryWriteResult(RegistryWriteStatus.Ok, RegistryHiveKind.CurrentUser, Path, null, _openBatch);
    }

    public void Dispose()
    {
        if (_disposed)
            return;
        if (_openBatch is not null)
            _ = CommitBatch("aborted");
        _writer.Dispose();
        _disposed = true;
    }

    private void Write(Dictionary<string, object?> row)
    {
        _writer.WriteLine(JsonSerializer.Serialize(row));
        _writer.Flush();
    }

    private static RegistryWriteResult Denied(string path, string reason)
        => new(RegistryWriteStatus.Denied, RegistryHiveKind.CurrentUser, path, null, reason);
}
