using Vestigium.Helpers;
using Vestigium.Helpers.Hashing;
using Vestigium.Logging;

namespace Vestigium.Helpers.FileIo;

/// <summary>
/// Validated file and directory jobs. Robocopy is the behavior reference; FileIo is the record.
/// Recon + five buckets, UniqueName default, Audit Mode, Pause/Cancel, ALCOA+ JSONL.
/// Category Helpers, APPID FileIo. Never spawns robocopy.exe. Never ReadAllBytes on a payload.
/// </summary>
public static class FileIoHelper
{
    public static string Identity => "Vestigium.Helpers.FileIo";

    public static string Probe()
    {
        var app = HelperLog.AppIds.FileIo;
        using var scope = HelperLog.Begin(app, HelperLog.Subcategories.Probe, "Probe");
        HelperLog.Information(app, VestigiumStatus.Pending, HelperLog.Subcategories.Probe, "Resolving a demo path under %TEMP%.");
        var root = Path.Combine(Path.GetTempPath(), "Vestigium.Helpers.FileIo.Probe", Guid.NewGuid().ToString("N"));
        try
        {
            var export = Path.Combine(root, "Export");
            var archive = Path.Combine(root, "Archive");
            Directory.CreateDirectory(export);
            Directory.CreateDirectory(archive);
            File.WriteAllText(Path.Combine(export, "nathan.txt"), "nathan capture\n");
            File.WriteAllText(Path.Combine(archive, "nathan.txt"), "older nathan\n");
            var job = Copy(export, archive, new FileIoJobOptions { ReconLeadTime = TimeSpan.Zero });
            var result = job.RunAsync().GetAwaiter().GetResult();
            if (result.Status != "Success" || !File.Exists(Path.Combine(archive, "nathan.01.txt")))
                throw new InvalidOperationException("FileIo probe UniqueName failed.");
            var audit = Copy(export, archive, new FileIoJobOptions { ReconLeadTime = TimeSpan.Zero, AuditMode = true });
            _ = audit.RunAsync().GetAwaiter().GetResult();
        }
        finally
        {
            try { Directory.Delete(root, true); } catch (IOException) { }
        }
        HelperLog.Information(app, VestigiumStatus.Success, HelperLog.Subcategories.Probe, "FileIo probe complete. Identity=" + Identity);
        return Identity;
    }

    public static FileIoJob Copy(string source, string destination, FileIoJobOptions? options = null)
        => FileIoJob.Copy(source, destination, options);

    public static FileIoJob Move(string source, string destination, FileIoJobOptions? options = null)
        => FileIoJob.Move(source, destination, options);

    public static FileIoJob Delete(string path, FileIoJobOptions? options = null)
        => FileIoJob.Delete(path, options);

    public static FileIoJob Mirror(string source, string destination, FileIoJobOptions? options = null)
        => FileIoJob.Mirror(source, destination, options);

    public static FileIoCompareResult CompareFiles(string left, string right, HashingAlgorithm algorithm = HashingAlgorithm.Sha256)
    {
        var leftMissing = !File.Exists(left);
        var rightMissing = !File.Exists(right);
        if (leftMissing || rightMissing)
        {
            FileIoLog.Failed(HelperLog.Subcategories.Compare, $"Compare missing left={leftMissing} right={rightMissing}");
            return new FileIoCompareResult
            {
                Equal = false,
                LeftDigest = "",
                RightDigest = "",
                LeftMissing = leftMissing,
                RightMissing = rightMissing,
            };
        }
        var l = HashingHelper.HashFile(left, algorithm);
        var r = HashingHelper.HashFile(right, algorithm);
        var equal = string.Equals(l, r, StringComparison.Ordinal);
        FileIoLog.Success(HelperLog.Subcategories.Compare, $"Compare equal={equal} left={l[..Math.Min(12, l.Length)]}…");
        return new FileIoCompareResult { Equal = equal, LeftDigest = l, RightDigest = r };
    }

    public static int PruneEmptyDirectories(string root)
    {
        var path = HelperGuard.NotBlank(root, nameof(root));
        if (!Directory.Exists(path))
            return 0;
        var n = Prune(path, isRoot: true);
        if (n > 0)
            FileIoLog.Success(HelperLog.Subcategories.Prune, $"Prune empty dirs={n} root={path}");
        return n;
    }

    public static void SecureDelete(string path, FileIoShredRecipe recipe)
    {
        var file = HelperGuard.FileExists(path, nameof(path));
        ArgumentNullException.ThrowIfNull(recipe);
        FileIoLog.Pending(HelperLog.Subcategories.SecureDelete, $"Shred path={file} passes={recipe}");
        var buffer = System.Buffers.ArrayPool<byte>.Shared.Rent(FileIoJob.StreamBufferSize);
        try
        {
            var length = new FileInfo(file).Length;
            foreach (var pass in recipe.Passes)
            {
                using var stream = new FileStream(file, FileMode.Open, FileAccess.Write, FileShare.None, FileIoJob.StreamBufferSize);
                Fill(buffer, pass);
                var remaining = length;
                while (remaining > 0)
                {
                    var n = (int)Math.Min(buffer.Length, remaining);
                    stream.Write(buffer, 0, n);
                    remaining -= n;
                }
                stream.Flush(true);
            }
        }
        finally
        {
            System.Buffers.ArrayPool<byte>.Shared.Return(buffer, clearArray: true);
            File.Delete(file);
        }
        FileIoLog.Success(HelperLog.Subcategories.SecureDelete, $"Shred path={file}");
    }

    public static void CleanIndex(string destinationRoot)
    {
        var dest = HelperGuard.NotBlank(destinationRoot, nameof(destinationRoot));
        var file = IndexPath(dest);
        if (File.Exists(file))
            File.Delete(file);
        FileIoLog.Success(HelperLog.Subcategories.Index, $"CleanIndex dest={dest}");
    }

    public static void CleanIndexesOlderThan(TimeSpan age)
    {
        var root = IndexRoot();
        if (!Directory.Exists(root))
            return;
        var cutoff = DateTime.UtcNow - age;
        foreach (var file in Directory.GetFiles(root, "*.jsonl"))
        {
            if (File.GetLastWriteTimeUtc(file) < cutoff)
                File.Delete(file);
        }
        FileIoLog.Success(HelperLog.Subcategories.Index, $"CleanIndexesOlderThan age={age}");
    }

    static string IndexRoot() =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Vestigium", "FileIo", "Indexes");

    static string IndexPath(string destinationRoot)
    {
        var hash = HashingHelper.HashString(Path.GetFullPath(destinationRoot));
        Directory.CreateDirectory(IndexRoot());
        return Path.Combine(IndexRoot(), hash + ".jsonl");
    }

    static int Prune(string dir, bool isRoot)
    {
        var n = 0;
        foreach (var child in Directory.GetDirectories(dir))
            n += Prune(child, false);
        if (!isRoot && Directory.GetFileSystemEntries(dir).Length == 0)
        {
            Directory.Delete(dir);
            n++;
        }
        return n;
    }

    static void Fill(byte[] buffer, string pass)
    {
        if (pass.Equals("Zero", StringComparison.OrdinalIgnoreCase))
            Array.Clear(buffer);
        else
            Random.Shared.NextBytes(buffer);
    }
}
