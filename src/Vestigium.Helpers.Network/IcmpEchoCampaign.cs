using Vestigium.Helpers;
using Vestigium.Helpers.Json;

namespace Vestigium.Helpers.Network;

public sealed class IcmpEchoCampaign
{
    private IcmpEchoCampaign(string campaignId, IcmpEchoCampaignOptions options)
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
        var confined = CampaignPaths.Confine(recipePath, nameof(recipePath));
        var path = HelperGuard.FileExists(confined, nameof(recipePath));
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
            CampaignJsonl.AppendCampaign(resultsPath, new
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
                CampaignJsonl.AppendCampaign(resultsPath, new
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
                NetworkLog.WindowMissed($"missed campaign={CampaignId} date={dateKey} time={timeKey}");
                continue;
            }

            CampaignJsonl.AppendCampaign(resultsPath, new
            {
                kind = "windowStart",
                campaignId = CampaignId,
                recordedUtc = nowUtc,
                date = dateKey,
                localTime = timeKey,
                count = window.Count,
                durationMs = window.Duration is { } dur ? (int)dur.TotalMilliseconds : (int?)null
            });

            var echoOptions = CloneEcho(Options.Echo);
            echoOptions.Count = window.Count;
            if (window.Duration is { } duration)
                echoOptions.MaxDuration = duration;
            echoOptions.StatsPath = null;
            var job = NetworkHelper.IcmpEcho(Options.Target, echoOptions);
            var result = await job.RunAsync(cancellation).ConfigureAwait(false);
            foreach (var reply in result.Replies)
            {
                CampaignJsonl.AppendCampaign(resultsPath, new
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

            CampaignJsonl.AppendCampaign(resultsPath, new
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
                CampaignJsonl.AppendCampaign(resultsPath, new
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

    private static IcmpEchoCampaignOptions Guard(IcmpEchoCampaignOptions? options)
    {
        var o = options ?? throw new ArgumentNullException(nameof(options));
        HelperGuard.NotBlank(o.Target, nameof(o.Target));
        EgressBind.Validate(o.Echo.InterfaceIndex, o.Echo.SourceAddress);
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

        foreach (var window in o.Windows)
        {
            if (window.Count < 1 && window.Duration is null)
            {
                HelperLog.Reject(HelperLog.AppIds.Network, HelperLog.Subcategories.Campaign, nameof(Guard), "window neither count nor duration");
                throw new ArgumentOutOfRangeException(nameof(o.Windows), "Window needs a Count of at least 1 or a Duration.");
            }

            if (window.Count < 0)
            {
                HelperLog.Reject(HelperLog.AppIds.Network, HelperLog.Subcategories.Campaign, nameof(Guard), "window count");
                throw new ArgumentOutOfRangeException(nameof(o.Windows), "Window Count cannot be negative.");
            }

            if (window.Duration is { } duration && (duration <= TimeSpan.Zero || duration > IcmpEchoOptions.MaxJobDuration))
            {
                HelperLog.Reject(HelperLog.AppIds.Network, HelperLog.Subcategories.Campaign, nameof(Guard), $"window duration={duration}");
                throw new ArgumentOutOfRangeException(nameof(o.Windows), "Window Duration must be greater than 0 and at most 24 hours.");
            }
        }

        if (o.Grace < TimeSpan.Zero || o.Grace > TimeSpan.FromHours(12))
        {
            HelperLog.Reject(HelperLog.AppIds.Network, HelperLog.Subcategories.Campaign, nameof(Guard), "grace");
            throw new ArgumentOutOfRangeException(nameof(o.Grace), "Grace must be between 0 and 12 hours.");
        }

        if (!string.IsNullOrWhiteSpace(o.RecipePath))
            o.RecipePath = CampaignPaths.Confine(o.RecipePath, nameof(o.RecipePath));
        if (!string.IsNullOrWhiteSpace(o.ResultsPath))
            o.ResultsPath = CampaignPaths.Confine(o.ResultsPath, nameof(o.ResultsPath));

        return o;
    }

    private static TimeZoneInfo ResolveZone(string? id)
    {
        return string.IsNullOrWhiteSpace(id) ? TimeZoneInfo.Local : TimeZoneInfo.FindSystemTimeZoneById(id);
    }

    private string ResolveResultsPath()
    {
        if (!string.IsNullOrWhiteSpace(Options.ResultsPath))
            return CampaignPaths.Confine(Options.ResultsPath, nameof(Options.ResultsPath));

        var root = CampaignPaths.Root();
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

    private static void WriteRecipe(string path, string campaignId, IcmpEchoCampaignOptions options)
    {
        CampaignPaths.EnsureDirectoryUnderRoot(path);
        JsonHelper.WriteFile(path, new CampaignRecipe
        {
            CampaignId = campaignId,
            Target = options.Target,
            RangeStartDate = options.RangeStartDate.ToString("yyyy-MM-dd"),
            RangeEndDate = options.RangeEndDate.ToString("yyyy-MM-dd"),
            TimeZoneId = options.TimeZoneId,
            GraceMinutes = (int)options.Grace.TotalMinutes,
            Windows = options.Windows.Select(w => new CampaignWindowDto
            {
                LocalTime = w.LocalTime.ToString("HH:mm"),
                Count = w.Count,
                DurationMs = w.Duration is { } d ? (int)d.TotalMilliseconds : null
            }).ToList(),
            ResultsPath = options.ResultsPath,
            Echo = CampaignEchoDto.From(options.Echo)
        }, new JsonWriteOptions { WriteIndented = true, Collision = JsonCollision.Overwrite });
    }

    private static IcmpEchoOptions CloneEcho(IcmpEchoOptions source)
        => new()
        {
            Count = source.Count,
            Timeout = source.Timeout,
            Interval = source.Interval,
            BufferSize = source.BufferSize,
            Ttl = source.Ttl,
            DontFragment = source.DontFragment,
            MaxDuration = source.MaxDuration,
            AllowBurst = source.AllowBurst,
            InterfaceIndex = source.InterfaceIndex,
            SourceAddress = source.SourceAddress
        };

    private sealed class CampaignRecipe
    {
        public string CampaignId { get; init; } = "";
        public string Target { get; init; } = "";
        public string RangeStartDate { get; init; } = "";
        public string RangeEndDate { get; init; } = "";
        public string? TimeZoneId { get; init; }
        public int GraceMinutes { get; init; } = 15;
        public List<CampaignWindowDto> Windows { get; init; } = [];
        public string? ResultsPath { get; init; }
        public CampaignEchoDto? Echo { get; init; }

        public IcmpEchoCampaignOptions ToOptions()
            => new()
            {
                Target = Target,
                RangeStartDate = DateOnly.Parse(RangeStartDate),
                RangeEndDate = DateOnly.Parse(RangeEndDate),
                TimeZoneId = TimeZoneId,
                Grace = TimeSpan.FromMinutes(GraceMinutes),
                Windows = Windows.Select(w => new EchoWindow(
                    TimeOnly.Parse(w.LocalTime),
                    w.Count,
                    w.DurationMs is { } ms ? TimeSpan.FromMilliseconds(ms) : null)).ToList(),
                ResultsPath = ResultsPath,
                Echo = Echo?.ToOptions() ?? new IcmpEchoOptions()
            };
    }

    private sealed class CampaignWindowDto
    {
        public string LocalTime { get; init; } = "";
        public int Count { get; init; }
        public int? DurationMs { get; init; }
    }

    private sealed class CampaignEchoDto
    {
        public int TimeoutMs { get; init; }
        public int IntervalMs { get; init; }
        public int BufferSize { get; init; }
        public int Ttl { get; init; }
        public bool DontFragment { get; init; }
        public int? MaxDurationMs { get; init; }
        public bool AllowBurst { get; init; }
        public int? InterfaceIndex { get; init; }
        public string? SourceAddress { get; init; }

        public static CampaignEchoDto From(IcmpEchoOptions echo)
            => new()
            {
                TimeoutMs = (int)echo.Timeout.TotalMilliseconds,
                IntervalMs = (int)echo.Interval.TotalMilliseconds,
                BufferSize = echo.BufferSize,
                Ttl = echo.Ttl,
                DontFragment = echo.DontFragment,
                MaxDurationMs = echo.MaxDuration is { } duration ? (int)duration.TotalMilliseconds : null,
                AllowBurst = echo.AllowBurst,
                InterfaceIndex = echo.InterfaceIndex >= 1 ? echo.InterfaceIndex : null,
                SourceAddress = string.IsNullOrWhiteSpace(echo.SourceAddress) ? null : echo.SourceAddress.Trim()
            };

        public IcmpEchoOptions ToOptions()
            => new()
            {
                Timeout = TimeSpan.FromMilliseconds(TimeoutMs),
                Interval = TimeSpan.FromMilliseconds(IntervalMs),
                BufferSize = BufferSize,
                Ttl = Ttl,
                DontFragment = DontFragment,
                MaxDuration = MaxDurationMs is { } ms ? TimeSpan.FromMilliseconds(ms) : null,
                AllowBurst = AllowBurst,
                InterfaceIndex = InterfaceIndex ?? 0,
                SourceAddress = SourceAddress
            };
    }
}
