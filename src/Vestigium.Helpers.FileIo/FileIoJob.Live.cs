using System.Collections.Concurrent;

namespace Vestigium.Helpers.FileIo;

public sealed partial class FileIoJob
{
    private readonly ConcurrentDictionary<string, FileIoFileProgress> _live = new(StringComparer.OrdinalIgnoreCase);
    private DateTimeOffset _lastLive = DateTimeOffset.MinValue;

    private void Enter(WorkItem item)
    {
        _live[item.RelativePath] = new FileIoFileProgress
        {
            RelativePath = item.RelativePath,
            Bucket = item.Bucket.ToString(),
            Size = item.Size,
            Status = "Active"
        };
        Snapshot(true);
    }

    private void Leave(WorkItem item)
    {
        _live.TryRemove(item.RelativePath, out _);
        Snapshot(false);
    }

    private void Stay(WorkItem item, string reason)
    {
        if (_live.TryGetValue(item.RelativePath, out var row))
        {
            row.Status = "Failed";
            row.Reason = reason;
        }
        Snapshot(false);
    }

    private void NoteBytes(string relative, long done)
    {
        if (_live.TryGetValue(relative, out var row))
            row.BytesDone = done;
        Snapshot(false);
    }

    private void Snapshot(bool now)
    {
        var stamp = DateTimeOffset.UtcNow;
        if (!now && stamp - _lastLive < TimeSpan.FromMilliseconds(250))
            return;
        _lastLive = stamp;
        Progress.Active = _live.Values.ToArray();
        if (!now)
            Emit();
    }
}
