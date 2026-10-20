namespace Vestigium.Helpers.Kql.AppMgmt;

public static class PackageAdapter
{
    public static KqlRow ToRow(KqlSession session, PackageFacts facts)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(facts);

        var row = new KqlRow(session)
            .Set("app.displayname", facts.DisplayName)
            .Set("app.publisher", facts.Publisher)
            .Set("app.version", facts.Version)
            .Set("app.MajorVersion", facts.MajorVersion)
            .Set("app.MinorVersion", facts.MinorVersion)
            .Set("app.IsX64", facts.IsX64)
            .Set("app.IsX86", !facts.IsX64)
            .Set("app.IsGlobal", facts.IsGlobal)
            .Set("app.IsSystemComponent", facts.IsSystemComponent)
            .Set("app.WindowsInstaller", facts.WindowsInstaller)
            .Set("app.NoModify", facts.NoModify)
            .Set("app.NoRemove", facts.NoRemove)
            .Set("app.NoRepair", facts.NoRepair)
            .Set("app.HasIcon", facts.HasIcon)
            .Set("app.HasModify", facts.HasModify)
            .Set("app.HasRepair", facts.HasRepair)
            .Set("app.IsOrphan", facts.IsOrphan)
            .Set("app.BaselineStatus", facts.BaselineStatus)
            .Set("app.PendingReboot", facts.PendingReboot);

        if (KqlDates.TryParse(facts.InstallDate, out var installed))
            row.Set("app.InstallDate", installed);

        if (facts.EstimatedSizeKb is { } kb)
        {
            row.Set("app.size.kb", kb)
                .Set("app.size.mb", kb / 1024d)
                .Set("app.size.gb", kb / 1024d / 1024d);
        }

        return row;
    }
}
