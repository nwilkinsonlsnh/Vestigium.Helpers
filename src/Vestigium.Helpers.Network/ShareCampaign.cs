using Vestigium.Helpers.FileIo;
using Vestigium.Helpers.Json;

namespace Vestigium.Helpers.Network;

public sealed class ShareCampaign
{
    public const string DefaultDisclaimer =
        "Linear scale from a sequential write probe (default 64 MiB × 4). Many-small-file trees need Advanced.";

    private ShareCampaign(string campaignId, ShareCampaignOptions options)
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
        return Task.FromResult(Options.Mode == ShareCampaignMode.Advanced
            ? ShareCampaignEngine.RunAdvanced(CampaignId, Options, cancellation)
            : ShareCampaignEngine.RunDefault(CampaignId, Options, cancellation));
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

        if (o.Mode == ShareCampaignMode.Default && o.ProbeBytes * (long)o.ProbeCount > o.MaxProbeBytes)
        {
            HelperLog.Reject(HelperLog.AppIds.Network, HelperLog.Subcategories.Share, nameof(Guard), "probe budget");
            throw new ArgumentException("ProbeBytes × ProbeCount cannot exceed MaxProbeBytes.", nameof(o.MaxProbeBytes));
        }

        if (o.Mode == ShareCampaignMode.Default && o.PlannedSize is null)
        {
            HelperLog.Reject(HelperLog.AppIds.Network, HelperLog.Subcategories.Share, nameof(Guard), "planned size missing");
            throw new ArgumentException("Default mode requires PlannedSize.", nameof(o.PlannedSize));
        }

        if (o.Mode == ShareCampaignMode.Advanced && o.SourceAnalysis is null)
        {
            HelperLog.Reject(HelperLog.AppIds.Network, HelperLog.Subcategories.Share, nameof(Guard), "analysis missing");
            throw new ArgumentException("Advanced mode requires SourceAnalysis.", nameof(o.SourceAnalysis));
        }

        if (!string.IsNullOrWhiteSpace(o.RecipePath))
            o.RecipePath = CampaignPaths.Confine(o.RecipePath, nameof(o.RecipePath));
        if (!string.IsNullOrWhiteSpace(o.ResultsPath))
            o.ResultsPath = CampaignPaths.Confine(o.ResultsPath, nameof(o.ResultsPath));

        return o;
    }

    private static void WriteRecipe(string path, string campaignId, ShareCampaignOptions options)
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

    private sealed class ShareRecipe
    {
        public string CampaignId { get; init; } = "";
        public string ShareDirectory { get; init; } = "";
        public string Mode { get; init; } = "Default";
        public double Efficiency { get; init; } = 1;
        public long MaxProbeBytes { get; init; }
        public long ProbeBytes { get; init; } = 64L * 1024 * 1024;
        public int ProbeCount { get; init; } = 4;
        public decimal? PlannedBits { get; init; }
        public decimal? DeclaredPipeBits { get; init; }
        public string? ResultsPath { get; init; }

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
