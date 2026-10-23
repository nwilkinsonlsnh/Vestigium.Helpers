using System.Collections.Concurrent;

namespace Vestigium.Helpers.FileIo;

public sealed partial class FileIoJob
{
    private readonly ConcurrentDictionary<string, FileIoFileProgress> _live = new(StringComparer.OrdinalIgnoreCase);

    private void Enter(WorkItem item)
    {
        _live[item.RelativePath] = new FileIoFileProgress
        {
            RelativePath = item.RelativePath,
            Bucket = item.Bucket.ToString(),
            Size = item.Size,
            Status = "Active"
        };
        Snapshot();
    }

    private void Leave(WorkItem item)
    {
        _live.TryRemove(item.RelativePath, out _);
        Snapshot();
    }

    private void Stay(WorkItem item, string reason)
    {
        if (_live.TryGetValue(item.RelativePath, out var row))
        {
            row.Status = "Failed";
            row.Reason = reason;
        }
        Snapshot();
    }

    private void NoteBytes(string relative, long done)
    {
        if (_live.TryGetValue(relative, out var row))
            row.BytesDone = done;
        Snapshot();
    }

    private void Snapshot()
        => Progress.Active = _live.Values.ToArray();
}
