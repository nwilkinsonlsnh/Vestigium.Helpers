using Vestigium.Helpers;
using Vestigium.Helpers.Json;

namespace Vestigium.Helpers.Network;

public sealed class IcmpEchoCampaign
{
    IcmpEchoCampaign(string campaignId, IcmpEchoCampaignOptions options)
    {
        CampaignId = campaignId;
        Options = options;
    }

    public string CampaignId { get; }
    public IcmpEchoCampaignOptions Options { get; }

    public static IcmpEchoCampaign Create(IcmpEchoCampaignOptions options)
    {
        var o = Guard(options);
        var id = "camp-" + HelperLog.NewId();
        if (!string.IsNullOrWhiteSpace(o.RecipePath))
            WriteRecipe(o.RecipePath, id, o);
        return new IcmpEchoCampaign(id, o);
    }

    public static IcmpEchoCampaign Open(string recipePath)
    {
        var path = HelperGuard.FileExists(recipePath, nameof(recipePath));
        var stored = JsonHelper.FromJson<CampaignRecipe>(File.ReadAllText(path));
        var options = stored.ToOptions();
        options.RecipePath = path;
        return new IcmpEchoCampaign(stored.CampaignId, Guard(options));
    }

    public async Task<IcmpEchoCampaignResult> RunAsync(CancellationToken cancellation = default)
    {
        using var scope = NetworkLog.Begin(HelperLog.Subcategories.Campaign, nameof(RunAsync), CampaignId);
        var tz = ResolveZone(Options.TimeZoneId);
        var nowUtc = NetworkTestHooks.Now();
        var nowLocal = TimeZoneInfo.ConvertTimeFromUtc(nowUtc.UtcDateTime, tz);
        var today = DateOnly.FromDateTime(nowLocal);
        var resultsPath = ResolveResultsPath();

        NetworkLog.Pending(
            HelperLog.Subcategories.Campaign,
            $"start campaign={CampaignId} target={Options.Target} today={today:yyyy-MM-dd} results={resultsPath}");

        var existing = CampaignJsonl.Read(resultsPath);
        if (!CampaignJsonl.HasKind(existing, CampaignId, "campaignStart"))
        {
            CampaignJsonl.Append(resultsPath, new
            {
                kind = "campaignStart",
                campaignId = CampaignId,
                recordedUtc = nowUtc,
                target = Options.Target,
                rangeStart = Options.RangeStartDate.ToString("yyyy-MM-dd"),
                rangeEnd = Options.RangeEndDate.ToString("yyyy-MM-dd")
            });
        }

        var run = 0;
        var missed = 0;
        var skipped = 0;
        var echoes = 0;

        if (today < Options.RangeStartDate || today > Options.RangeEndDate)
        {
            NetworkLog.Warning(HelperLog.Subcategories.Campaign, $"campaign={CampaignId} today outside range");
            return new IcmpEchoCampaignResult(CampaignId, NetworkJobStatus.Success, 0, 0, 0, 0, resultsPath);
        }

        foreach (var window in Options.Windows.OrderBy(w => w.LocalTime))
        {
            cancellation.ThrowIfCancellationRequested();
            var dateKey = today.ToString("yyyy-MM-dd");
            var timeKey = window.LocalTime.ToString("HH:mm");
            if (CampaignJsonl.HasTerminalWindow(existing, CampaignId, dateKey, timeKey))
            {
                skipped++;
                continue;
            }

            var scheduled = DateTime.SpecifyKind(today.ToDateTime(window.LocalTime), DateTimeKind.Unspecified);
            var scheduledLocal = TimeZoneInfo.ConvertTimeToUtc(scheduled, tz);
            var dueAt = new DateTimeOffset(scheduledLocal, TimeSpan.Zero);
            if (nowUtc < dueAt)
            {
                skipped++;
                continue;
            }

            if (nowUtc - dueAt > Options.Grace)
            {
                CampaignJsonl.Append(resultsPath, new
                {
                    kind = "windowMissed",
                    campaignId = CampaignId,
                    recordedUtc = nowUtc,
                    date = dateKey,
                    localTime = timeKey,
                    count = window.Count
                });
                existing = CampaignJsonl.Read(resultsPath);
                missed++;
                NetworkLog.Warning(HelperLog.Subcategories.Campaign, $"missed campaign={CampaignId} date={dateKey} time={timeKey}");
                continue;
            }

            CampaignJsonl.Append(resultsPath, new
            {
                kind = "windowStart",
                campaignId = CampaignId,
                recordedUtc = nowUtc,
                date = dateKey,
                localTime = timeKey,
                count = window.Count
            });

            var echoOptions = CloneEcho(Options.Echo);
            echoOptions.Count = window.Count;
            echoOptions.StatsPath = null;
            var job = NetworkHelper.IcmpEcho(Options.Target, echoOptions);
            var result = await job.RunAsync(cancellation).ConfigureAwait(false);
            foreach (var reply in result.Replies)
            {
                CampaignJsonl.Append(resultsPath, new
                {
                    kind = "echo",
                    campaignId = CampaignId,
                    recordedUtc = NetworkTestHooks.Now(),
                    date = dateKey,
                    localTime = timeKey,
                    sequence = reply.Sequence,
                    status = reply.Status.ToString(),
                    address = reply.Address,
                    rttMs = reply.RoundtripTimeMs
                });
                echoes++;
            }

            CampaignJsonl.Append(resultsPath, new
            {
                kind = "windowSummary",
                campaignId = CampaignId,
                recordedUtc = NetworkTestHooks.Now(),
                date = dateKey,
                localTime = timeKey,
                sent = result.Sent,
                received = result.Received,
                lost = result.Lost,
                status = result.Status.ToString()
            });
            existing = CampaignJsonl.Read(resultsPath);
            run++;
        }

        if (today >= Options.RangeEndDate && Options.Windows.All(w =>
                CampaignJsonl.HasTerminalWindow(existing, CampaignId, today.ToString("yyyy-MM-dd"), w.LocalTime.ToString("HH:mm"))
                || nowUtc < TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(today.ToDateTime(w.LocalTime), DateTimeKind.Unspecified), tz)))
        {
            if (!CampaignJsonl.HasKind(existing, CampaignId, "campaignEnd") && missed + run + skipped >= Options.Windows.Count)
            {
                CampaignJsonl.Append(resultsPath, new
                {
                    kind = "campaignEnd",
                    campaignId = CampaignId,
                    recordedUtc = NetworkTestHooks.Now()
                });
            }
        }

        NetworkLog.Success(
            HelperLog.Subcategories.Campaign,
            $"end campaign={CampaignId} run={run} missed={missed} skipped={skipped} echoes={echoes}");
        return new IcmpEchoCampaignResult(CampaignId, NetworkJobStatus.Success, run, missed, skipped, echoes, resultsPath);
    }

    static IcmpEchoCampaignOptions Guard(IcmpEchoCampaignOptions? options)
    {
        var o = options ?? throw new ArgumentNullException(nameof(options));
        HelperGuard.NotBlank(o.Target, nameof(o.Target));
        if (o.RangeEndDate < o.RangeStartDate)
        {
            HelperLog.Reject(HelperLog.AppIds.Network, HelperLog.Subcategories.Campaign, nameof(Guard), "range inverted");
            throw new ArgumentOutOfRangeException(nameof(o.RangeEndDate), "RangeEndDate must be on or after RangeStartDate.");
        }

        if (o.Windows.Count == 0)
        {
            HelperLog.Reject(HelperLog.AppIds.Network, HelperLog.Subcategories.Campaign, nameof(Guard), "no windows");
            throw new ArgumentException("At least one window is required.", nameof(o.Windows));
        }

        if (o.Windows.Select(w => w.LocalTime).Distinct().Count() != o.Windows.Count)
        {
            HelperLog.Reject(HelperLog.AppIds.Network, HelperLog.Subcategories.Campaign, nameof(Guard), "duplicate LocalTime");
            throw new ArgumentException("Windows must have unique LocalTime values.", nameof(o.Windows));
        }

        if (o.Windows.Any(w => w.Count < 1))
        {
            HelperLog.Reject(HelperLog.AppIds.Network, HelperLog.Subcategories.Campaign, nameof(Guard), "window count");
            throw new ArgumentOutOfRangeException(nameof(o.Windows), "Window Count must be at least 1.");
        }

        if (o.Grace < TimeSpan.Zero || o.Grace > TimeSpan.FromHours(12))
        {
            HelperLog.Reject(HelperLog.AppIds.Network, HelperLog.Subcategories.Campaign, nameof(Guard), "grace");
            throw new ArgumentOutOfRangeException(nameof(o.Grace), "Grace must be between 0 and 12 hours.");
        }

        return o;
    }

    static TimeZoneInfo ResolveZone(string? id)
    {
        if (string.IsNullOrWhiteSpace(id))
            return TimeZoneInfo.Local;
        return TimeZoneInfo.FindSystemTimeZoneById(id);
    }

    string ResolveResultsPath()
    {
        if (!string.IsNullOrWhiteSpace(Options.ResultsPath))
            return Path.GetFullPath(Options.ResultsPath);
        var root = NetworkTestHooks.CampaignRoot;
        if (string.IsNullOrWhiteSpace(root))
        {
            root = OperatingSystem.IsWindows()
                ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Vestigium", "Network", "Campaigns")
                : "/var/lib/vestigium/network/campaigns";
        }

        try
        {
            Directory.CreateDirectory(root);
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or IOException)
        {
            HelperLog.Reject(HelperLog.AppIds.Network, HelperLog.Subcategories.Campaign, nameof(ResolveResultsPath), "campaign root not writable");
            throw;
        }

        return Path.Combine(root, CampaignId + ".jsonl");
    }

    static void WriteRecipe(string path, string campaignId, IcmpEchoCampaignOptions options)
    {
        var dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrWhiteSpace(dir))
            Directory.CreateDirectory(dir);
        JsonHelper.WriteFile(path, new CampaignRecipe
        {
            CampaignId = campaignId,
            Target = options.Target,
            RangeStartDate = options.RangeStartDate.ToString("yyyy-MM-dd"),
            RangeEndDate = options.RangeEndDate.ToString("yyyy-MM-dd"),
            TimeZoneId = options.TimeZoneId,
            GraceMinutes = (int)options.Grace.TotalMinutes,
            Windows = options.Windows.Select(w => new CampaignWindowDto { LocalTime = w.LocalTime.ToString("HH:mm"), Count = w.Count }).ToList(),
            ResultsPath = options.ResultsPath
        }, new JsonWriteOptions { WriteIndented = true, Collision = JsonCollision.Overwrite });
    }

    static IcmpEchoOptions CloneEcho(IcmpEchoOptions source)
        => new()
        {
            Count = source.Count,
            Timeout = source.Timeout,
            Interval = source.Interval,
            BufferSize = source.BufferSize,
            Ttl = source.Ttl,
            DontFragment = source.DontFragment,
            MaxDuration = source.MaxDuration
        };

    sealed class CampaignRecipe
    {
        public string CampaignId { get; set; } = "";
        public string Target { get; set; } = "";
        public string RangeStartDate { get; set; } = "";
        public string RangeEndDate { get; set; } = "";
        public string? TimeZoneId { get; set; }
        public int GraceMinutes { get; set; } = 15;
        public List<CampaignWindowDto> Windows { get; set; } = [];
        public string? ResultsPath { get; set; }

        public IcmpEchoCampaignOptions ToOptions()
            => new()
            {
                Target = Target,
                RangeStartDate = DateOnly.Parse(RangeStartDate),
                RangeEndDate = DateOnly.Parse(RangeEndDate),
                TimeZoneId = TimeZoneId,
                Grace = TimeSpan.FromMinutes(GraceMinutes),
                Windows = Windows.Select(w => new EchoWindow(TimeOnly.Parse(w.LocalTime), w.Count)).ToList(),
                ResultsPath = ResultsPath
            };
    }

    sealed class CampaignWindowDto
    {
        public string LocalTime { get; set; } = "";
        public int Count { get; set; }
    }
}
