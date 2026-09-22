using Vestigium.Helpers.Hashing;

namespace Vestigium.Helpers.FileIo;

public enum FileIoCollision
{
    UniqueName = 0,
    Skip = 1,
    Overwrite = 2,
}

public enum FileIoVerb
{
    Copy = 1,
    Move = 2,
    Delete = 3,
    Mirror = 4,
}

public enum FileIoBucket
{
    Tiny = 0,
    Small = 1,
    Medium = 2,
    Large = 3,
    Huge = 4,
}

public sealed class FileIoAge
{
    public int? Days { get; set; }
    public DateTimeOffset? Date { get; set; }

    internal DateTimeOffset CutoffUtc()
    {
        if (Date is { } d)
            return d.ToUniversalTime();
        if (Days is { } days)
            return DateTimeOffset.UtcNow.AddDays(-days);
        return DateTimeOffset.MinValue;
    }
}

public sealed class FileIoShredRecipe
{
    public static FileIoShredRecipe ZeroRandomZero { get; } = new("Zero,Random,Zero");
    public static FileIoShredRecipe ThreeRandomThenZero { get; } = new("Random,Random,Random,Zero");
    public static FileIoShredRecipe SevenRandomThenZero { get; } = new("Random,Random,Random,Random,Random,Random,Random,Zero");

    internal string[] Passes { get; }

    FileIoShredRecipe(string passes) => Passes = passes.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    public override string ToString() => string.Join(",", Passes);
}

public sealed class FileIoJobOptions
{
    public FileIoCollision Collision { get; set; } = FileIoCollision.UniqueName;
    public string UniqueNamePattern { get; set; } = UniqueName.DefaultPattern;
    public bool CopyOnlyUniqueContent { get; set; }
    public HashingAlgorithm HashAlgorithm { get; set; } = HashingAlgorithm.Sha256;
    public bool AuditMode { get; set; }
    public TimeSpan ReconLeadTime { get; set; } = TimeSpan.FromSeconds(15);
    public bool IncludeEmptyDirectories { get; set; }
    public int? MaxDepth { get; set; }
    public IReadOnlyList<string> ExcludeFileMasks { get; set; } = [];
    public IReadOnlyList<string> ExcludeDirectoryMasks { get; set; } = [];
    public long? MinSizeBytes { get; set; }
    public long? MaxSizeBytes { get; set; }
    public FileIoAge? MinAge { get; set; }
    public FileIoAge? MaxAge { get; set; }
    public int RetryCount { get; set; } = 3;
    public TimeSpan RetryWait { get; set; } = TimeSpan.FromSeconds(2);
    public bool CopyTimestampsAndAttributes { get; set; } = true;
    public bool Purge { get; set; }
    public bool PruneEmptyDirectories { get; set; }
    public bool StopOnError { get; set; }
    public FileIoShredRecipe? Shred { get; set; }
    public string? RequestedBy { get; set; }
    public string? Reason { get; set; }
    public IProgress<FileIoProgress>? Progress { get; set; }
}

public sealed class FileIoBucketProgress
{
    public FileIoBucket Id { get; init; }
    public string Name { get; init; } = "";
    public int Found { get; set; }
    public int Queued { get; set; }
    public int Active { get; set; }
    public int Done { get; set; }
    public int Skipped { get; set; }
    public int Failed { get; set; }
    public long BytesFound { get; set; }
    public long BytesDone { get; set; }

    public static FileIoBucketProgress[] CreateFive() =>
    [
        new() { Id = FileIoBucket.Tiny, Name = "Tiny" },
        new() { Id = FileIoBucket.Small, Name = "Small" },
        new() { Id = FileIoBucket.Medium, Name = "Medium" },
        new() { Id = FileIoBucket.Large, Name = "Large" },
        new() { Id = FileIoBucket.Huge, Name = "Huge" },
    ];
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
}

public sealed class FileIoJobResult
{
    public required string JobId { get; init; }
    public required FileIoVerb Verb { get; init; }
    public required string Status { get; init; }
    public int Copied { get; init; }
    public int Skipped { get; init; }
    public int Failed { get; init; }
    public int Deleted { get; init; }
    public long Bytes { get; init; }
    public bool AuditMode { get; init; }
    public FileIoJobStats? Stats { get; init; }
}

public sealed class FileIoCompareResult
{
    public required bool Equal { get; init; }
    public required string LeftDigest { get; init; }
    public required string RightDigest { get; init; }
    public bool LeftMissing { get; init; }
    public bool RightMissing { get; init; }
}
