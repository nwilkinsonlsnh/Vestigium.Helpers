using Vestigium.Helpers;
using Vestigium.Logging;

namespace Vestigium.Helpers.Kql;

/// <summary>KQL-inspired filter helpers. Phase 1: catalog and session.</summary>
public static class KqlHelper
{
    public static string Identity => "Vestigium.Helpers.Kql";

    public static string Probe()
    {
        var app = HelperLog.AppIds.Kql;
        HelperLog.Information(app, VestigiumStatus.Pending, app, "Describing the KQL filter catalog.");
        using var session = Create(KqlPack.Process);
        HelperLog.Information(
            app,
            VestigiumStatus.Success,
            app,
            $"KQL probe complete. Identity={Identity} fields={session.Fields.Count}");
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
}
