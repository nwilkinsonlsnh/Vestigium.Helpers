using System.Collections.Concurrent;

namespace Vestigium.Helpers.FileIo;

public sealed partial class FileIoJob
{
    private readonly ConcurrentDictionary<string, FileIoFileProgress> _live = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentQueue<FileIoFileProgress> _recent = new();
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
        Publish();
    }

    private void Leave(WorkItem item)
    {
        if (_live.TryRemove(item.RelativePath, out var row))
        {
            row.BytesDone = item.Size;
            row.Status = "Done";
            _recent.Enqueue(row);
            while (_recent.Count > 20)
                _recent.TryDequeue(out _);
        }
        Publish();
    }

    private void Stay(WorkItem item, string reason)
    {
        if (_live.TryGetValue(item.RelativePath, out var row))
        {
            row.Status = "Failed";
            row.Reason = reason;
        }
        Publish();
    }

    private void NoteBytes(string relative, long done)
    {
        if (_live.TryGetValue(relative, out var row))
            row.BytesDone = done;
        Publish();
    }

    private void Publish()
    {
        var stamp = DateTimeOffset.UtcNow;
        var shown = new List<FileIoFileProgress>(_live.Count + _recent.Count);
        shown.AddRange(_live.Values);
        shown.AddRange(_recent);
        Progress.Active = shown.ToArray();
        if (stamp - _lastLive < TimeSpan.FromMilliseconds(100))
            return;
        _lastLive = stamp;
        Emit();
    }
}
