public sealed class FileIoFileProgress
{
    public string RelativePath { get; init; } = "";
    public string Bucket { get; init; } = "";
    public long Size { get; init; }
    public long BytesDone { get; set; }
    public string Status { get; set; } = "Active";
    public string? Reason { get; set; }
}

public sealed class FileIoProgress
{
    public string JobId { get; init; } = "";
    public string Phase { get; set; } = "Recon";
    public bool IsPaused { get; set; }
    public bool ReconComplete { get; set; }
    public int CertaintyPercent { get; set; }
    public int FilesFound { get; set; }
    public int FilesDone { get; set; }
    public int FilesSkipped { get; set; }
    public int FilesFailed { get; set; }
    public long BytesFound { get; set; }
    public long BytesDone { get; set; }
    public long RateBytesPerSec { get; set; }
    public DateTimeOffset? EtaUtc { get; set; }
    public FileIoBucketProgress[] Buckets { get; set; } = FileIoBucketProgress.CreateFive();
    public FileIoFileProgress[] Active { get; set; } = [];
}