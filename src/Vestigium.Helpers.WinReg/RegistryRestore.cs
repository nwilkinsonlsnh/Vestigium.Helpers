using System.Text.Json;
using Vestigium.Helpers;

namespace Vestigium.Helpers.WinReg;

internal static class RegistryRestore
{
    public static RegistryWriteResult Run(
        RegistryClient client,
        string snapshot,
        bool confirm,
        RegistryJournal? journal,
        IProgress<RegistryCompareProgress>? progress,
        CancellationToken cancel)
    {
        snapshot = HelperGuard.FileExists(snapshot, nameof(snapshot));
        if (!confirm)
            return new RegistryWriteResult(RegistryWriteStatus.Denied, RegistryHiveKind.CurrentUser, snapshot, null, "confirm=false");

        var first = FirstNonEmpty(snapshot);
        if (first.StartsWith("Windows Registry Editor", StringComparison.OrdinalIgnoreCase)
            || first.Equals("REGEDIT4", StringComparison.OrdinalIgnoreCase))
        {
            journal?.EnsureBatch("restore-reg");
            return client.Import(snapshot, confirm: true, progress: progress, cancel: cancel, journal: journal);
        }

        if (first.Contains("vest-regidx/1", StringComparison.Ordinal))
        {
            journal?.EnsureBatch("restore-index");
            return ApplyIndex(client, snapshot, journal, progress, cancel);
        }

        return new RegistryWriteResult(RegistryWriteStatus.InvalidPath, RegistryHiveKind.CurrentUser, snapshot, null, "not a .reg or vest-regidx/1");
    }

    private static RegistryWriteResult ApplyIndex(
        RegistryClient client,
        string snapshot,
        RegistryJournal? journal,
        IProgress<RegistryCompareProgress>? progress,
        CancellationToken cancel)
    {
        RegistryHiveKind hive = RegistryHiveKind.CurrentUser;
        var root = "";
        var view = RegistryViewKind.Default;
        var applied = 0;
        var skipped = 0;

        foreach (var line in File.ReadLines(snapshot))
        {
            if (cancel.IsCancellationRequested)
                return new RegistryWriteResult(RegistryWriteStatus.Denied, hive, snapshot, null, "canceled");
            if (string.IsNullOrWhiteSpace(line))
                continue;
            using var doc = JsonDocument.Parse(line);
            var rec = doc.RootElement.GetProperty("rec").GetString();
            switch (rec)
            {
                case "header":
                    hive = Enum.Parse<RegistryHiveKind>(doc.RootElement.GetProperty("hive").GetString() ?? "CurrentUser");
                    root = doc.RootElement.GetProperty("path").GetString() ?? "";
                    if (doc.RootElement.TryGetProperty("view", out var viewEl))
                        view = Enum.TryParse<RegistryViewKind>(viewEl.GetString(), out var parsed) ? parsed : RegistryViewKind.Default;
                    break;
                case "key":
                {
                    var rel = doc.RootElement.GetProperty("path").GetString() ?? "";
                    var path = Combine(root, rel);
                    var created = client.CreateKey(hive, path, view, confirm: true, journal);
                    if (created.Status != RegistryWriteStatus.Ok && !string.IsNullOrEmpty(path))
                        return created;
                    applied++;
                    break;
                }
                case "value":
                {
                    var rel = doc.RootElement.GetProperty("path").GetString() ?? "";
                    var path = Combine(root, rel);
                    var name = doc.RootElement.GetProperty("name").GetString() ?? "";
                    if (!doc.RootElement.TryGetProperty("text", out var textEl) || textEl.ValueKind != JsonValueKind.String)
                    {
                        skipped++;
                        continue;
                    }

                    var kind = Enum.TryParse<RegistryValueKind>(doc.RootElement.GetProperty("type").GetString(), out var parsedKind)
                        ? parsedKind
                        : RegistryValueKind.String;
                    if (kind is not RegistryValueKind.String and not RegistryValueKind.ExpandString)
                    {
                        skipped++;
                        continue;
                    }

                    var set = client.SetValue(hive, path, name, textEl.GetString(), kind, view, confirm: true, journal);
                    if (set.Status != RegistryWriteStatus.Ok)
                        return set;
                    applied++;
                    break;
                }
            }

            if (applied % 25 == 0)
                progress?.Report(new RegistryCompareProgress { Phase = "Restore", KeysSeen = applied, ValuesSeen = skipped });
        }

        return new RegistryWriteResult(RegistryWriteStatus.Ok, hive, snapshot, null, $"applied={applied} skipped={skipped}");
    }

    private static string Combine(string root, string relative)
    {
        if (string.IsNullOrEmpty(relative))
            return root;
        return string.IsNullOrEmpty(root) ? relative : root + "\\" + relative;
    }

    private static string FirstNonEmpty(string path)
    {
        foreach (var line in File.ReadLines(path))
        {
            if (!string.IsNullOrWhiteSpace(line))
                return line.Trim();
        }
        return string.Empty;
    }
}
