using System.Management;
using System.ServiceProcess;
using Microsoft.Win32;

namespace Vestigium.Helpers.SystemInfo;

public static partial class SystemInventory
{
    private static readonly (RegistryHive Hive, string View, string Location, string Arch)[] Opens =
    [
        (RegistryHive.LocalMachine, "", "LM", "x64"),
        (RegistryHive.LocalMachine, "WOW6432Node\\", "LM", "x86"),
        (RegistryHive.CurrentUser, "", "CU", "x64"),
        (RegistryHive.CurrentUser, "WOW6432Node\\", "CU", "x86")
    ];

    /// <summary>
    /// Reads installed software, hotfixes, features, ODBC, environment, and SQL services.
    /// Each section is Fact.Unavailable on failure. Partial success is allowed.
    /// Hidden is a bool. Status and StartType stay numeric.
    /// </summary>
    public static SoftwareInventory ReadSoftware()
    {
        return new SoftwareInventory(
            ReadInstalled(),
            ReadHotfixes(),
            ReadFeatures(),
            ReadOdbc(),
            ReadEnvironment(),
            ReadSql());
    }

    private static Fact<IReadOnlyList<InstalledSoftware>> ReadInstalled()
    {
        try
        {
            var list = new List<InstalledSoftware>();
            foreach (var (hive, view, location, arch) in Opens)
            {
                var path = $"SOFTWARE\\{view}Microsoft\\Windows\\CurrentVersion\\Uninstall";
                using var root = RegistryKey.OpenBaseKey(hive, RegistryView.Registry64);
                using var key = root.OpenSubKey(path);
                if (key is null)
                    continue;
                foreach (var sub in key.GetSubKeyNames())
                {
                    using var child = key.OpenSubKey(sub);
                    if (child is null)
                        continue;
                    var display = child.GetValue("DisplayName")?.ToString();
                    var quiet = child.GetValue("QuietDisplayName")?.ToString();
                    var name = !string.IsNullOrWhiteSpace(display) ? display
                        : !string.IsNullOrWhiteSpace(quiet) ? quiet
                        : sub;
                    var system = child.GetValue("SystemComponent")?.ToString() == "1";
                    var childOf = !string.IsNullOrWhiteSpace(child.GetValue("ParentKeyName")?.ToString());
                    var noDisplay = string.IsNullOrWhiteSpace(display);
                    list.Add(new InstalledSoftware(
                        location,
                        arch,
                        name ?? "",
                        child.GetValue("DisplayVersion")?.ToString() ?? "",
                        child.GetValue("Publisher")?.ToString() ?? "",
                        child.GetValue("InstallDate")?.ToString() ?? "",
                        system || childOf || noDisplay));
                }
            }
            return Fact<IReadOnlyList<InstalledSoftware>>.Ok(list);
        }
        catch (Exception)
        {
            return Fact<IReadOnlyList<InstalledSoftware>>.Unavailable();
        }
    }

    private static Fact<IReadOnlyList<Hotfix>> ReadHotfixes()
    {
        try
        {
            var list = new List<Hotfix>();
            foreach (var mo in Query("SELECT HotFixID, Description, InstalledOn, InstalledBy FROM Win32_QuickFixEngineering"))
            {
                using (mo)
                {
                    list.Add(new Hotfix(
                        String(mo, "HotFixID") ?? "",
                        String(mo, "Description") ?? "",
                        String(mo, "InstalledOn") ?? "",
                        String(mo, "InstalledBy") ?? ""));
                }
            }
            return Fact<IReadOnlyList<Hotfix>>.Ok(list);
        }
        catch (Exception)
        {
            return Fact<IReadOnlyList<Hotfix>>.Unavailable();
        }
    }

    private static Fact<IReadOnlyList<OptionalFeature>> ReadFeatures()
    {
        try
        {
            var list = new List<OptionalFeature>();
            foreach (var mo in Query("SELECT Name, InstallState FROM Win32_OptionalFeature"))
            {
                using (mo)
                {
                    list.Add(new OptionalFeature(
                        String(mo, "Name") ?? "",
                        UInt(mo, "InstallState")));
                }
            }
            return Fact<IReadOnlyList<OptionalFeature>>.Ok(list);
        }
        catch (Exception)
        {
            return Fact<IReadOnlyList<OptionalFeature>>.Unavailable();
        }
    }

    private static Fact<IReadOnlyList<OdbcEntry>> ReadOdbc()
    {
        try
        {
            var list = new List<OdbcEntry>();
            foreach (var (hive, view, location, arch) in Opens)
            {
                using var root = RegistryKey.OpenBaseKey(hive, RegistryView.Registry64);
                AddOdbc(list, root, $"SOFTWARE\\{view}ODBC\\ODBCINST.INI", location, arch, "Driver");
                AddOdbc(list, root, $"SOFTWARE\\{view}ODBC\\ODBC.INI", location, arch, "DSN");
            }
            return Fact<IReadOnlyList<OdbcEntry>>.Ok(list);
        }
        catch (Exception)
        {
            return Fact<IReadOnlyList<OdbcEntry>>.Unavailable();
        }
    }

    private static void AddOdbc(List<OdbcEntry> list, RegistryKey root, string path, string location, string arch, string kind)
    {
        using var key = root.OpenSubKey(path);
        if (key is null)
            return;
        foreach (var name in key.GetSubKeyNames())
        {
            if (string.Equals(name, "ODBC Drivers", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(name, "ODBC Data Sources", StringComparison.OrdinalIgnoreCase))
                continue;
            using var child = key.OpenSubKey(name);
            list.Add(new OdbcEntry(
                location,
                arch,
                kind,
                name,
                child?.GetValue("Driver")?.ToString() ?? ""));
        }
    }

    private static Fact<IReadOnlyList<EnvironmentEntry>> ReadEnvironment()
    {
        try
        {
            var list = new List<EnvironmentEntry>();
            AddEnv(list, "Process", Environment.GetEnvironmentVariables(EnvironmentVariableTarget.Process));
            AddEnv(list, "User", Environment.GetEnvironmentVariables(EnvironmentVariableTarget.User));
            AddEnv(list, "Machine", Environment.GetEnvironmentVariables(EnvironmentVariableTarget.Machine));
            return Fact<IReadOnlyList<EnvironmentEntry>>.Ok(list);
        }
        catch (Exception)
        {
            return Fact<IReadOnlyList<EnvironmentEntry>>.Unavailable();
        }
    }

    private static void AddEnv(List<EnvironmentEntry> list, string scope, System.Collections.IDictionary vars)
    {
        foreach (System.Collections.DictionaryEntry entry in vars)
        {
            list.Add(new EnvironmentEntry(
                scope,
                entry.Key?.ToString() ?? "",
                entry.Value?.ToString() ?? ""));
        }
    }

    private static Fact<IReadOnlyList<SqlService>> ReadSql()
    {
        try
        {
            var list = new List<SqlService>();
            foreach (var svc in ServiceController.GetServices())
            {
                using (svc)
                {
                    var name = svc.ServiceName;
                    if (!name.StartsWith("MSSQL", StringComparison.OrdinalIgnoreCase) &&
                        !name.StartsWith("SQL", StringComparison.OrdinalIgnoreCase))
                        continue;
                    list.Add(new SqlService(
                        name,
                        svc.DisplayName,
                        (uint)svc.Status,
                        (uint)svc.StartType));
                }
            }
            return Fact<IReadOnlyList<SqlService>>.Ok(list);
        }
        catch (Exception)
        {
            return Fact<IReadOnlyList<SqlService>>.Unavailable();
        }
    }
}
