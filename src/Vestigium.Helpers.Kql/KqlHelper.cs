using Vestigium.Helpers;
using Vestigium.Logging;

namespace Vestigium.Helpers.Kql;

/// <summary>KQL-inspired filter helpers. Phase 2: catalog, session, parser.</summary>
public static class KqlHelper
{
    public static string Identity => "Vestigium.Helpers.Kql";

    public static string Probe()
    {
        var app = HelperLog.AppIds.Kql;
        HelperLog.Information(app, VestigiumStatus.Pending, app, "Describing the KQL filter catalog.");
        using var session = Create(KqlPack.Process);
        var parsed = Parse("PID == 0");
        HelperLog.Information(
            app,
            VestigiumStatus.Success,
            app,
            $"KQL probe complete. Identity={Identity} fields={session.Fields.Count} parseOk={parsed.Ok}");
        return Identity;
    }

    public static KqlSession Create(params KqlPack[] packs)
        => Create(new KqlOptions { Packs = packs is { Length: > 0 } ? packs : [KqlPack.Process] });

    public static KqlSession Create(KqlOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        var packs = options.Packs is { Count: > 0 } ? options.Packs : [KqlPack.Process];
        var fields = KqlCatalog.For(packs, options.Groups);
        var groups = options.Groups == KqlGroups.None
            ? KqlCatalog.DefaultGroups(packs)
            : options.Groups;
        return new KqlSession(packs, groups, fields);
    }

    public static KqlParseResult Parse(string text)
    {
        var result = KqlParser.Parse(text);
        if (!result.Ok && result.Error is { } error)
        {
            HelperLog.Warning(
                HelperLog.AppIds.Kql,
                VestigiumStatus.Failed,
                HelperLog.Subcategories.Query,
                $"parse failed line={error.Line} col={error.Column} chars={(text ?? string.Empty).Length}");
        }

        return result;
    }
}
