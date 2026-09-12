using System.Globalization;
using System.Runtime.Versioning;
using Microsoft.Win32;
using Vestigium.Helpers;
using Vestigium.Logging;

namespace Vestigium.Helpers.WinReg;

[SupportedOSPlatform("windows")]
public sealed class RegistryClient
{
    internal RegistryClient(string? machine) => Machine = RegistryPath.NormalizeMachine(machine);

    public string? Machine { get; }
    public bool IsLocal => Machine is null;

    public RegistryKeyInfo? GetKey(
        RegistryHiveKind hive,
        string? key,
        RegistryViewKind view = RegistryViewKind.Default,
        RegistryDetailLevel level = RegistryDetailLevel.Slim)
    {
        var path = RegistryPath.Normalize(key);
        try
        {
            using var opened = Open(hive, path, view, writable: false);
            if (opened is null)
                return null;
            return Materialize(hive, path, view, opened, level);
        }
        catch (UnauthorizedAccessException)
        {
            return new RegistryKeyInfo
            {
                Machine = Machine,
                Hive = hive,
                Path = path,
                Name = RegistryPath.Leaf(path),
                View = view,
                Availability = [new RegistryFieldAvailability("Key", "Denied", "UnauthorizedAccess")]
            };
        }
        catch (IOException)
        {
            return null;
        }
    }

    public bool TryGetKey(
        RegistryHiveKind hive,
        string? key,
        out RegistryKeyInfo? info,
        RegistryViewKind view = RegistryViewKind.Default,
        RegistryDetailLevel level = RegistryDetailLevel.Slim)
    {
        info = GetKey(hive, key, view, level);
        return info is not null && info.Availability.All(a => a.State != "Denied");
    }

    public RegistryValueInfo? GetValue(
        RegistryHiveKind hive,
        string? key,
        string? valueName,
        RegistryViewKind view = RegistryViewKind.Default,
        bool expand = false)
    {
        var path = RegistryPath.Normalize(key);
        var name = valueName ?? string.Empty;
        try
        {
            using var opened = Open(hive, path, view, writable: false);
            if (opened is null)
                return null;
            var options = expand ? RegistryValueOptions.None : RegistryValueOptions.DoNotExpandEnvironmentNames;
            var data = opened.GetValue(name, null, options);
            if (data is null && opened.GetValueKind(name) is var _)
            {
            }

            try
            {
                var kind = MapKind(opened.GetValueKind(name));
                data = opened.GetValue(name, null, options);
                return new RegistryValueInfo
                {
                    Name = name,
                    IsDefault = name.Length == 0,
                    Type = kind,
                    Data = data,
                    DataText = Format(data, kind)
                };
            }
            catch (IOException)
            {
                return null;
            }
        }
        catch (UnauthorizedAccessException)
        {
            return null;
        }
        catch (IOException)
        {
            return null;
        }
    }

    public bool TryGetValue(
        RegistryHiveKind hive,
        string? key,
        string? valueName,
        out RegistryValueInfo? info,
        RegistryViewKind view = RegistryViewKind.Default,
        bool expand = false)
    {
        info = GetValue(hive, key, valueName, view, expand);
        return info is not null;
    }

    public IReadOnlyList<RegistryKeyInfo> ListSubKeys(
        RegistryHiveKind hive,
        string? key,
        RegistryViewKind view = RegistryViewKind.Default,
        RegistryDetailLevel level = RegistryDetailLevel.Slim)
    {
        var path = RegistryPath.Normalize(key);
        try
        {
            using var opened = Open(hive, path, view, writable: false);
            if (opened is null)
                return [];
            var names = opened.GetSubKeyNames();
            var rows = new List<RegistryKeyInfo>(names.Length);
            foreach (var name in names.OrderBy(n => n, StringComparer.OrdinalIgnoreCase))
            {
                var child = string.IsNullOrEmpty(path) ? name : path + "\\" + name;
                if (level == RegistryDetailLevel.Identity)
                {
                    rows.Add(new RegistryKeyInfo
                    {
                        Machine = Machine,
                        Hive = hive,
                        Path = child,
                        Name = name,
                        View = view
                    });
                    continue;
                }

                var snap = GetKey(hive, child, view, level);
                if (snap is not null)
                    rows.Add(snap);
            }

            HelperLog.Information(HelperLog.AppIds.WinReg, VestigiumStatus.Success, HelperLog.Subcategories.Inventory, $"ListSubKeys hive={hive} path={path} n={rows.Count}");
            return rows;
        }
        catch (UnauthorizedAccessException)
        {
            return [];
        }
        catch (IOException)
        {
            return [];
        }
    }

    internal RegistryKey? Open(RegistryHiveKind hive, string path, RegistryViewKind view, bool writable)
    {
        RegistryKey? root = null;
        try
        {
            var mapped = MapHive(hive);
            var mappedView = MapView(view);
            root = Machine is null
                ? RegistryKey.OpenBaseKey(mapped, mappedView)
                : RegistryKey.OpenRemoteBaseKey(mapped, Machine, mappedView);
            if (string.IsNullOrEmpty(path))
                return root;
            var sub = root.OpenSubKey(path, writable);
            root.Dispose();
            return sub;
        }
        catch
        {
            root?.Dispose();
            return null;
        }
    }

    private RegistryKeyInfo Materialize(RegistryHiveKind hive, string path, RegistryViewKind view, RegistryKey opened, RegistryDetailLevel level)
    {
        var availability = new List<RegistryFieldAvailability>();
        int? subCount = null;
        int? valCount = null;
        string[] names = [];
        IReadOnlyList<RegistryValueInfo> values = [];

        if (level != RegistryDetailLevel.Identity)
        {
            try { subCount = opened.SubKeyCount; } catch { availability.Add(new("SubKeyCount", "Denied", "SubKeyCount")); }
            try { valCount = opened.ValueCount; } catch { availability.Add(new("ValueCount", "Denied", "ValueCount")); }
            try { names = opened.GetSubKeyNames(); } catch { availability.Add(new("SubKeyNames", "Denied", "GetSubKeyNames")); }
        }

        if (level == RegistryDetailLevel.Full)
        {
            try
            {
                var list = new List<RegistryValueInfo>();
                foreach (var name in opened.GetValueNames())
                {
                    var kind = MapKind(opened.GetValueKind(name));
                    var data = opened.GetValue(name, null, RegistryValueOptions.DoNotExpandEnvironmentNames);
                    list.Add(new RegistryValueInfo
                    {
                        Name = name,
                        IsDefault = name.Length == 0,
                        Type = kind,
                        Data = data,
                        DataText = Format(data, kind)
                    });
                }

                try
                {
                    var kind = MapKind(opened.GetValueKind(string.Empty));
                    var data = opened.GetValue(string.Empty, null, RegistryValueOptions.DoNotExpandEnvironmentNames);
                    if (data is not null || opened.GetValueNames().Any(n => n.Length == 0))
                    {
                        if (list.All(v => v.Name.Length != 0))
                        {
                            list.Insert(0, new RegistryValueInfo
                            {
                                Name = string.Empty,
                                IsDefault = true,
                                Type = kind,
                                Data = data,
                                DataText = Format(data, kind)
                            });
                        }
                    }
                }
                catch (IOException)
                {
                }

                values = list;
            }
            catch
            {
                availability.Add(new("Values", "Denied", "GetValueNames"));
            }
        }

        return new RegistryKeyInfo
        {
            Machine = Machine,
            Hive = hive,
            Path = path,
            Name = RegistryPath.Leaf(path),
            View = view,
            SubKeyCount = subCount,
            ValueCount = valCount,
            SubKeyNames = names,
            Values = values,
            Availability = availability
        };
    }

    private static RegistryHive MapHive(RegistryHiveKind hive) => hive switch
    {
        RegistryHiveKind.ClassesRoot => RegistryHive.ClassesRoot,
        RegistryHiveKind.CurrentUser => RegistryHive.CurrentUser,
        RegistryHiveKind.LocalMachine => RegistryHive.LocalMachine,
        RegistryHiveKind.Users => RegistryHive.Users,
        RegistryHiveKind.CurrentConfig => RegistryHive.CurrentConfig,
        _ => throw new ArgumentOutOfRangeException(nameof(hive))
    };

    private static RegistryView MapView(RegistryViewKind view) => view switch
    {
        RegistryViewKind.Registry64 => RegistryView.Registry64,
        RegistryViewKind.Registry32 => RegistryView.Registry32,
        _ => RegistryView.Default
    };

    private static RegistryValueKind MapKind(Microsoft.Win32.RegistryValueKind kind) => kind switch
    {
        Microsoft.Win32.RegistryValueKind.String => RegistryValueKind.String,
        Microsoft.Win32.RegistryValueKind.ExpandString => RegistryValueKind.ExpandString,
        Microsoft.Win32.RegistryValueKind.Binary => RegistryValueKind.Binary,
        Microsoft.Win32.RegistryValueKind.DWord => RegistryValueKind.DWord,
        Microsoft.Win32.RegistryValueKind.MultiString => RegistryValueKind.MultiString,
        Microsoft.Win32.RegistryValueKind.QWord => RegistryValueKind.QWord,
        Microsoft.Win32.RegistryValueKind.None => RegistryValueKind.None,
        _ => RegistryValueKind.Unknown
    };

    private static string? Format(object? data, RegistryValueKind kind) => data switch
    {
        null => null,
        string text => text,
        string[] parts => string.Join("\\0", parts),
        int number => number.ToString(CultureInfo.InvariantCulture),
        long qword => qword.ToString(CultureInfo.InvariantCulture),
        byte[] bytes => Convert.ToHexString(bytes),
        _ => Convert.ToString(data, CultureInfo.InvariantCulture)
    };
}
