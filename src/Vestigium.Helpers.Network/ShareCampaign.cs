using Vestigium.Helpers.FileIo;
using Vestigium.Helpers.Json;

namespace Vestigium.Helpers.Network;

public sealed class ShareCampaign
{
    public const string DefaultDisclaimer =
        "Linear scale from a sequential write probe (default 64 MiB × 4). Many-small-file trees need Advanced.";

    ShareCampaign(string campaignId, ShareCampaignOptions options)
    {
        CampaignId = campaignId;
        Options = options;
    }

    public string CampaignId { get; }
    public ShareCampaignOptions Options { get; }

    public static ShareCampaign Create(ShareCampaignOptions options)
    {
        var o = Guard(options);
        var id = "share-" + HelperLog.NewId();
        if (!string.IsNullOrWhiteSpace(o.RecipePath))
            WriteRecipe(o.RecipePath, id, o);
        return new ShareCampaign(id, o);
    }

    public static ShareCampaign Open(string recipePath)
    {
        var confined = CampaignPaths.Confine(recipePath, nameof(recipePath));
        var path = HelperGuard.FileExists(confined, nameof(recipePath));
        var stored = JsonHelper.FromJson<ShareRecipe>(File.ReadAllText(path));
        var options = stored.ToOptions();
        options.RecipePath = path;
        return new ShareCampaign(stored.CampaignId, Guard(options));
    }

    public Task<ShareCampaignResult> RunAsync(CancellationToken cancellation = default)
    {
        cancellation.ThrowIfCancellationRequested();
        if (Options.Mode == ShareCampaignMode.Advanced)
            throw new InvalidOperationException("Advanced share estimate is PR03.004 / PR03.005.");
        return Task.FromResult(ShareCampaignEngine.RunDefault(CampaignId, Options, cancellation));
    }

    internal static ShareCampaignOptions Guard(ShareCampaignOptions? options)
    {
        var o = options ?? throw new ArgumentNullException(nameof(options));
        o.Target ??= new FileShareTarget();
        o.Target.Directory = SharePaths.Confine(o.Target.Directory, nameof(o.Target.Directory));

        if (o.Efficiency is <= 0 or > 1)
        {
            HelperLog.Reject(HelperLog.AppIds.Network, HelperLog.Subcategories.Share, nameof(Guard), $"efficiency={o.Efficiency}");
            throw new ArgumentOutOfRangeException(nameof(o.Efficiency), "Efficiency must be greater than 0 and at most 1.0.");
        }

        if (o.MaxProbeBytes < 1024 * 1024)
        {
            HelperLog.Reject(HelperLog.AppIds.Network, HelperLog.Subcategories.Share, nameof(Guard), $"maxProbe={o.MaxProbeBytes}");
            throw new ArgumentOutOfRangeException(nameof(o.MaxProbeBytes), "MaxProbeBytes must be at least 1 MiB.");
        }

        if (o.ProbeBytes < 1)
        {
            HelperLog.Reject(HelperLog.AppIds.Network, HelperLog.Subcategories.Share, nameof(Guard), $"probeBytes={o.ProbeBytes}");
            throw new ArgumentOutOfRangeException(nameof(o.ProbeBytes), "ProbeBytes must be at least 1.");
        }

        if (o.ProbeCount is < 1 or > 16)
        {
            HelperLog.Reject(HelperLog.AppIds.Network, HelperLog.Subcategories.Share, nameof(Guard), $"probeCount={o.ProbeCount}");
            throw new ArgumentOutOfRangeException(nameof(o.ProbeCount), "ProbeCount must be between 1 and 16.");
        }

        if (o.ProbeBytes * (long)o.ProbeCount > o.MaxProbeBytes)
        {
            HelperLog.Reject(HelperLog.AppIds.Network, HelperLog.Subcategories.Share, nameof(Guard), "probe budget");
            throw new ArgumentException("ProbeBytes × ProbeCount cannot exceed MaxProbeBytes.", nameof(o.MaxProbeBytes));
        }

        if (o.Mode == ShareCampaignMode.Default && o.PlannedSize is null)
        {
            HelperLog.Reject(HelperLog.AppIds.Network, HelperLog.Subcategories.Share, nameof(Guard), "planned size missing");
            throw new ArgumentException("Default mode requires PlannedSize.", nameof(o.PlannedSize));
        }

        if (!string.IsNullOrWhiteSpace(o.RecipePath))
            o.RecipePath = CampaignPaths.Confine(o.RecipePath, nameof(o.RecipePath));
        if (!string.IsNullOrWhiteSpace(o.ResultsPath))
            o.ResultsPath = CampaignPaths.Confine(o.ResultsPath, nameof(o.ResultsPath));

        return o;
    }

    static void WriteRecipe(string path, string campaignId, ShareCampaignOptions options)
    {
        CampaignPaths.EnsureDirectoryUnderRoot(path);
        JsonHelper.WriteFile(path, new ShareRecipe
        {
            CampaignId = campaignId,
            ShareDirectory = options.Target.Directory,
            Mode = options.Mode.ToString(),
            Efficiency = options.Efficiency,
            MaxProbeBytes = options.MaxProbeBytes,
            ProbeBytes = options.ProbeBytes,
            ProbeCount = options.ProbeCount,
            PlannedBits = options.PlannedSize?.Bits,
            DeclaredPipeBits = options.DeclaredPipeRate?.Bits,
            ResultsPath = options.ResultsPath
        }, new JsonWriteOptions { WriteIndented = true, Collision = JsonCollision.Overwrite });
    }

    sealed class ShareRecipe
    {
        public string CampaignId { get; set; } = "";
        public string ShareDirectory { get; set; } = "";
        public string Mode { get; set; } = "Default";
        public double Efficiency { get; set; } = 1;
        public long MaxProbeBytes { get; set; }
        public long ProbeBytes { get; set; } = 64L * 1024 * 1024;
        public int ProbeCount { get; set; } = 4;
        public decimal? PlannedBits { get; set; }
        public decimal? DeclaredPipeBits { get; set; }
        public string? ResultsPath { get; set; }

        public ShareCampaignOptions ToOptions()
            => new()
            {
                Target = new FileShareTarget { Directory = ShareDirectory },
                Mode = Enum.TryParse<ShareCampaignMode>(Mode, true, out var mode) ? mode : ShareCampaignMode.Default,
                Efficiency = Efficiency,
                MaxProbeBytes = MaxProbeBytes,
                ProbeBytes = ProbeBytes <= 0 ? 64L * 1024 * 1024 : ProbeBytes,
                ProbeCount = ProbeCount <= 0 ? 4 : ProbeCount,
                PlannedSize = PlannedBits is { } bits ? BandwidthEngine.From(bits, DataUnit.Bit) : null,
                DeclaredPipeRate = DeclaredPipeBits is { } pipe ? BandwidthEngine.From(pipe, DataUnit.Bit) : null,
                ResultsPath = ResultsPath
            };
    }
}

internal static class SharePaths
{
    public static string Confine(string directory, string paramName)
    {
        var raw = HelperGuard.NotBlank(directory, paramName);
        string full;
        try
        {
            full = Path.GetFullPath(raw);
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
        {
            HelperLog.Reject(HelperLog.AppIds.Network, HelperLog.Subcategories.Share, nameof(Confine), "bad share path");
            throw new ArgumentException("Share directory is not a valid path.", paramName, ex);
        }

        if (!string.IsNullOrWhiteSpace(NetworkTestHooks.ShareRoot)
            && !CampaignPaths.IsUnder(full, NetworkTestHooks.ShareRoot))
        {
            HelperLog.Reject(HelperLog.AppIds.Network, HelperLog.Subcategories.Share, nameof(Confine), "share escape");
            throw new ArgumentException("Share directory must stay under the allowed share root.", paramName);
        }

        return full;
    }
}

internal static class ShareProbePlanner
{
    public static ShareProbePlan Plan(FileIoDirectoryAnalysis source, ShareProbeOptions? options)
    {
        ArgumentNullException.ThrowIfNull(source);
        var o = options ?? new ShareProbeOptions();
        if (o.MaxProbeBytes < 1024 * 1024)
            throw new ArgumentOutOfRangeException(nameof(options), "MaxProbeBytes must be at least 1 MiB.");

        return new ShareProbePlan
        {
            Mode = ShareCampaignMode.Advanced,
            SourcePath = source.Path,
            PlannedBytes = source.TotalBytes,
            MaxProbeBytes = o.MaxProbeBytes,
            Probes = []
        };
    }
}

internal static class ShareCampaignEngine
{
    public static ShareCampaignResult RunDefault(string campaignId, ShareCampaignOptions options, CancellationToken cancellation)
    {
        using var scope = NetworkLog.Begin(HelperLog.Subcategories.Share, nameof(RunDefault), campaignId);
        var planned = options.PlannedSize ?? throw new InvalidOperationException("Default mode requires PlannedSize.");
        var resultsPath = ResolveResults(campaignId, options);
        var samples = CollectRates(options, cancellation);
        var bits = samples.Select(b => (decimal)(b * 8d)).ToArray();
        var bill = PercentileBillEngine.FromSamples(bits, 0.95);
        var transfer = BandwidthEngine.TransferTime(planned, bill.Rate);
        var measured = TimeSpan.FromTicks((long)Math.Round(transfer.Duration.Ticks / options.Efficiency, MidpointRounding.AwayFromZero));
        TimeSpan? declared = options.DeclaredPipeRate is { } pipe
            ? BandwidthEngine.TransferTime(planned, pipe).Duration
            : null;

        CampaignJsonl.AppendCampaign(resultsPath, new
        {
            kind = "campaignStart",
            campaignId,
            mode = "Default",
            recordedUtc = NetworkTestHooks.Now()
        });
        CampaignJsonl.AppendCampaign(resultsPath, new
        {
            kind = "probe",
            campaignId,
            count = samples.Count,
            p95BitsPerSecond = bill.Rate.Bits,
            recordedUtc = NetworkTestHooks.Now()
        });
        CampaignJsonl.AppendCampaign(resultsPath, new
        {
            kind = "campaignEnd",
            campaignId,
            measuredSeconds = measured.TotalSeconds,
            recordedUtc = NetworkTestHooks.Now()
        });

        NetworkLog.Success(
            HelperLog.Subcategories.Share,
            $"default campaign={campaignId} p95={bill.Rate.Display} duration={measured}");

        return new ShareCampaignResult(
            campaignId,
            NetworkJobStatus.Success,
            ShareCampaignMode.Default,
            resultsPath,
            ShareCampaign.DefaultDisclaimer,
            measured,
            bill.Rate,
            declared);
    }

    static IReadOnlyList<double> CollectRates(ShareCampaignOptions options, CancellationToken cancellation)
    {
        if (NetworkTestHooks.ProbeBytesPerSecond is { Count: > 0 } injected)
            return injected;

        var size = FileIoSize.FromBytes(options.ProbeBytes);
        var rates = new List<double>(options.ProbeCount);
        for (var i = 0; i < options.ProbeCount; i++)
        {
            cancellation.ThrowIfCancellationRequested();
            var written = FileIoHelper.WriteProbe(options.Target.Directory, size);
            rates.Add(written.BytesPerSecond);
            if (options.IncludeReadProbe && !written.Deleted)
            {
                var read = FileIoHelper.ReadProbe(written.Path);
                rates.Add(read.BytesPerSecond);
            }
        }

        return rates;
    }

    static string ResolveResults(string campaignId, ShareCampaignOptions options)
    {
        if (!string.IsNullOrWhiteSpace(options.ResultsPath))
            return CampaignPaths.Confine(options.ResultsPath, nameof(options.ResultsPath));

        var root = CampaignPaths.Root();
        Directory.CreateDirectory(root);
        return Path.Combine(root, campaignId + ".jsonl");
    }
}
