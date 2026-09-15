using Vestigium.Helpers.WinReg;

namespace Vestigium.Helpers.Tests;

[Collection("Logger")]
public sealed class Branch90ClientWalkTests : IDisposable
{
    private readonly RegistryHiveKind _hive = RegistryHiveKind.CurrentUser;
    private readonly string _root = @"Software\Vestigium\Helpers.Tests\" + Guid.NewGuid().ToString("N");
    private readonly string _dir = Path.Combine(Path.GetTempPath(), "vest-walk-" + Guid.NewGuid().ToString("N"));
    private readonly RegistryClient _c = RegistryHelper.Local;

    public Branch90ClientWalkTests()
    {
        Directory.CreateDirectory(_dir);
        Assert.Equal(RegistryWriteStatus.Ok, _c.CreateKey(_hive, _root, confirm: true).Status);
        Assert.Equal(RegistryWriteStatus.Ok, _c.CreateKey(_hive, _root + "\\Child", confirm: true).Status);
        _c.SetValue(_hive, _root, "Name", "Alpha", confirm: true);
        _c.SetValue(_hive, _root, "Tail", "Omega", confirm: true);
        _c.SetValue(_hive, _root, "DwordU", 7u, RegistryValueKind.DWord, confirm: true);
        _c.SetValue(_hive, _root, "QFromInt", 3, RegistryValueKind.QWord, confirm: true);
        _c.SetValue(_hive, _root, "Exp", "%TEMP%", RegistryValueKind.ExpandString, confirm: true);
        _c.SetValue(_hive, _root, "Multi", new[] { "one", "two" }, RegistryValueKind.MultiString, confirm: true);
        _c.SetValue(_hive, _root + "\\Child", "Inner", "needle", confirm: true);
    }

    public void Dispose()
    {
        _c.DeleteKey(_hive, _root, recursive: true, confirm: true);
        try { Directory.Delete(_dir, true); } catch { }
    }

    [Fact]
    public void Get_list_search_views()
    {
        Assert.NotNull(_c.GetKey(_hive, _root, RegistryViewKind.Default, RegistryDetailLevel.Identity));
        Assert.NotNull(_c.GetKey(_hive, _root, RegistryViewKind.Registry64, RegistryDetailLevel.Slim));
        var full = _c.GetKey(_hive, _root, RegistryViewKind.Registry32, RegistryDetailLevel.Full);
        Assert.NotNull(full);
        Assert.NotEmpty(full!.Values);
        Assert.True(_c.TryGetKey(_hive, _root, out _));
        Assert.False(_c.TryGetKey(_hive, _root + "\\missing", out _));
        Assert.Null(_c.GetKey(_hive, _root + "\\missing"));

        Assert.NotNull(_c.GetValue(_hive, _root, "Name"));
        Assert.NotNull(_c.GetValue(_hive, _root, "Exp", expand: true));
        Assert.True(_c.TryGetValue(_hive, _root, "Name", out _));
        Assert.False(_c.TryGetValue(_hive, _root, "nope", out _));

        Assert.NotEmpty(_c.ListSubKeys(_hive, _root, level: RegistryDetailLevel.Identity));
        Assert.NotEmpty(_c.ListSubKeys(_hive, _root, level: RegistryDetailLevel.Slim));
        Assert.Empty(_c.ListSubKeys(_hive, _root + "\\missing"));

        _ = _c.Search(_hive, _root, "Alp", RegistrySearchMode.StartsWith, RegistrySearchFields.ValueData);
        _ = _c.Search(_hive, _root, "ega", RegistrySearchMode.EndsWith, RegistrySearchFields.ValueData);
        _ = _c.Search(_hive, _root, "Child", RegistrySearchMode.Contains, RegistrySearchFields.KeyName);
        _ = _c.Search(_hive, _root, "Name", RegistrySearchMode.Contains, RegistrySearchFields.ValueName);
        _ = _c.Search(_hive, _root, "need", RegistrySearchMode.Contains, RegistrySearchFields.ValueData, maxDepth: 2, maxResults: 4);
        _ = _c.Search(_hive, _root, "A", 0);
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        _ = _c.Search(_hive, _root, "A", cancel: cts.Token);
        Assert.Throws<ArgumentException>(() => _c.Search(_hive, _root, "A", maxDepth: 99));
        Assert.Throws<ArgumentException>(() => _c.Search(_hive, _root, "A", maxResults: 0));

        _ = RegistryClient.MapHive(RegistryHiveKind.ClassesRoot);
        _ = RegistryClient.MapHive(RegistryHiveKind.Users);
        _ = RegistryClient.MapHive(RegistryHiveKind.CurrentConfig);
        _ = RegistryClient.MapView(RegistryViewKind.Registry64);
        _ = RegistryClient.MapView(RegistryViewKind.Registry32);
        _ = RegistryClient.MapView(RegistryViewKind.Default);
        Assert.Throws<ArgumentOutOfRangeException>(() => RegistryClient.MapHive((RegistryHiveKind)99));
    }

    [Fact]
    public void Copy_rename_export_import_acl()
    {
        Assert.Equal(RegistryWriteStatus.Denied, _c.CopyKey(_hive, _root, _hive, _root + "\\Copy", confirm: false).Status);
        Assert.Equal(RegistryWriteStatus.InvalidPath, _c.CopyKey(_hive, "", _hive, _root + "\\Copy", confirm: true).Status);
        Assert.Equal(RegistryWriteStatus.NotFound, _c.CopyKey(_hive, _root + "\\gone", _hive, _root + "\\CopyMissing", confirm: true).Status);

        var copyTo = _root + "\\Copy";
        Assert.Equal(RegistryWriteStatus.Ok, _c.CopyKey(_hive, _root + "\\Child", _hive, copyTo, confirm: true, progress: new Progress<RegistryCompareProgress>(_ => { })).Status);

        Assert.Equal(RegistryWriteStatus.Denied, _c.RenameKey(_hive, copyTo, _root + "\\Renamed", confirm: false).Status);
        Assert.Equal(RegistryWriteStatus.NotFound, _c.RenameKey(_hive, _root + "\\gone", _root + "\\Renamed", confirm: true).Status);
        Assert.Equal(RegistryWriteStatus.InUse, _c.RenameKey(_hive, copyTo, _root + "\\Child", confirm: true).Status);
        var renamed = _root + "\\RenamedChild";
        var rename = _c.RenameKey(_hive, copyTo, renamed, confirm: true);
        Assert.True(rename.Status is RegistryWriteStatus.Ok or RegistryWriteStatus.Denied);

        Assert.Equal(RegistryWriteStatus.Denied, _c.RenameValue(_hive, _root, "Name", "Name2", confirm: false).Status);
        Assert.Equal(RegistryWriteStatus.InvalidPath, _c.RenameValue(_hive, _root, "Name", "Name", confirm: true).Status);
        Assert.Equal(RegistryWriteStatus.NotFound, _c.RenameValue(_hive, _root, "nope", "Name2", confirm: true).Status);
        Assert.Equal(RegistryWriteStatus.Ok, _c.RenameValue(_hive, _root, "Tail", "Tail2", confirm: true).Status);
        Assert.Equal(RegistryWriteStatus.InUse, _c.RenameValue(_hive, _root, "Name", "Tail2", confirm: true).Status);

        Assert.Equal(RegistryWriteStatus.Denied, _c.Export(Path.Combine(_dir, "x.reg"), _hive, _root, confirm: false).Status);
        Assert.Equal(RegistryWriteStatus.NotFound, _c.Export(Path.Combine(_dir, "x.reg"), _hive, _root + "\\gone", confirm: true).Status);
        using var exportCancel = new CancellationTokenSource();
        exportCancel.Cancel();
        Assert.Equal(RegistryWriteStatus.Denied, _c.Export(Path.Combine(_dir, "x.reg"), _hive, _root, confirm: true, cancel: exportCancel.Token).Status);

        var reg = Path.Combine(_dir, "out.reg");
        Assert.Equal(RegistryWriteStatus.Ok, _c.Export(reg, _hive, _root, RegistryExportFormat.RegFile, confirm: true, progress: new Progress<RegistryCompareProgress>(_ => { })).Status);
        _ = _c.Export(Path.Combine(_dir, "out.hiv"), _hive, _root, RegistryExportFormat.HiveFile, confirm: true);

        Assert.Equal(RegistryWriteStatus.Denied, _c.Import(reg, confirm: false).Status);
        using var importCancel = new CancellationTokenSource();
        importCancel.Cancel();
        Assert.Equal(RegistryWriteStatus.Denied, _c.Import(reg, confirm: true, cancel: importCancel.Token).Status);

        var empty = Path.Combine(_dir, "empty.reg");
        File.WriteAllText(empty, "");
        Assert.Equal(RegistryWriteStatus.InvalidPath, _c.Import(empty, confirm: true).Status);
        var badHead = Path.Combine(_dir, "bad.reg");
        File.WriteAllText(badHead, "NOT A HEADER\r\n");
        Assert.Equal(RegistryWriteStatus.InvalidPath, _c.Import(badHead, confirm: true).Status);
        var valueFirst = Path.Combine(_dir, "vf.reg");
        File.WriteAllText(valueFirst, "Windows Registry Editor Version 5.00\r\n\"A\"=\"b\"\r\n");
        Assert.Equal(RegistryWriteStatus.InvalidPath, _c.Import(valueFirst, confirm: true).Status);
        Assert.Equal(RegistryWriteStatus.Denied, _c.Import(reg, confirm: true, allowedHives: [RegistryHiveKind.LocalMachine]).Status);

        var importOk = Path.Combine(_dir, "ok.reg");
        File.WriteAllText(importOk,
            "Windows Registry Editor Version 5.00\r\n\r\n; comment\r\n[HKEY_CURRENT_USER\\" + _root.Replace("\\", "\\") + "\\Imported]\r\n\"Hi\"=\"there\"\r\n\"Hi\"=-\r\n");
        var imported = _c.Import(importOk, confirm: true, progress: new Progress<RegistryCompareProgress>(_ => { }));
        Assert.True(imported.Status is RegistryWriteStatus.Ok or RegistryWriteStatus.Denied);

        Assert.Equal(RegistryWriteStatus.Denied, _c.SetOwner(_hive, _root, Environment.UserName, confirm: false).Status);
        Assert.Equal(RegistryWriteStatus.InvalidPath, _c.SetOwner(_hive, _root, "NoSuchAccount_Vestigium_ZZZ", confirm: true).Status);
        _ = _c.SetOwner(_hive, _root, Environment.UserName, confirm: true);
        Assert.Equal(RegistryWriteStatus.Denied, _c.TakeOwnership(_hive, _root, confirm: false).Status);
        _ = _c.TakeOwnership(_hive, _root, confirm: true);
        Assert.Equal(RegistryWriteStatus.Denied, _c.SetSddl(_hive, _root, "D:(A;;KA;;;WD)", confirm: false).Status);
        Assert.Equal(RegistryWriteStatus.InvalidPath, _c.SetSddl(_hive, _root, "O:WD", confirm: true).Status);
        _ = _c.SetSddl(_hive, _root, "D:(A;;KA;;;WD)", confirm: true);

        Assert.Equal(RegistryWriteStatus.Denied, _c.WriteIndex(Path.Combine(_dir, "i.jsonl"), _hive, _root, confirm: false).Status);
        var idx = Path.Combine(_dir, "i.jsonl");
        Assert.Equal(RegistryWriteStatus.Ok, _c.WriteIndex(idx, _hive, _root, confirm: true, includePayload: true).Status);
        var idx2 = Path.Combine(_dir, "j.jsonl");
        File.Copy(idx, idx2);
        _ = RegistryHelper.Compare(idx, idx2, Path.Combine(_dir, "delta.jsonl"), confirm: true, includeSame: true, includePayload: true);

        Assert.Equal(RegistryWriteStatus.InvalidPath, _c.DeleteKey(_hive, _root, recursive: false, confirm: true).Status);
        Assert.Equal(RegistryWriteStatus.Denied, RegistryHelper.Apply(new RegistryEditList { Label = "x" }, Path.Combine(_dir, "a.jnl"), confirm: false).Status);
    }
}
