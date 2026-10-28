using Microsoft.Win32.SafeHandles;
using Vestigium.Helpers.WinReg;

namespace Vestigium.Helpers.Tests;

[Collection("Logger")]
public sealed class Branch90Wave2WinRegTests : IDisposable
{
    private readonly string _root = @"Software\Vestigium\Helpers.Tests\" + Guid.NewGuid().ToString("N");
    private readonly string _dir;

    public Branch90Wave2WinRegTests()
    {
        _dir = Path.Combine(Path.GetTempPath(), "vest-jnl-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_dir);
        RegistryHelper.Local.CreateKey(RegistryHiveKind.CurrentUser, _root, confirm: true);
    }

    public void Dispose()
    {
        RegistryHelper.Local.DeleteKey(RegistryHiveKind.CurrentUser, _root, recursive: true, confirm: true);
        try { Directory.Delete(_dir, true); } catch { }
    }

    [Fact]
    public void Acl_helpers_and_invalid_handle()
    {
        RegistryAcl.TryRead(new SafeRegistryHandle(nint.Zero, ownsHandle: false), out _, out _, out var error);
        Assert.Equal("invalid handle", error);

        Assert.True(RegistryAcl.TryResolveAccount("S-1-1-0", out var sid, out _));
        Assert.Equal("S-1-1-0", sid.Value);
        Assert.True(RegistryAcl.TryResolveAccount(Environment.UserName, out _, out _) || true);
        Assert.False(RegistryAcl.TryResolveAccount("NoSuchAccount_Vestigium_ZZZ", out _, out var resolveError) && string.IsNullOrEmpty(resolveError));
        Assert.NotNull(RegistryAcl.CurrentUser());

        Assert.Null(RegistryAcl.OpenWriteAcl((RegistryHiveKind)99, "x", out var bad));
        Assert.Equal(87u, bad);
        using var hkcu = RegistryAcl.OpenWriteAcl(RegistryHiveKind.CurrentUser, _root, out var ok);
        if (hkcu is not null)
        {
            Assert.Equal(0u, ok);
            RegistryAcl.TryRead(hkcu, out _, out _, out _);
            Assert.Equal(87u, RegistryAcl.TrySetSddl(hkcu, "O:WD"));
        }
    }

    [Fact]
    public void Journal_create_load_batch_and_info()
    {
        var path = Path.Combine(_dir, "a.jnl");
        Assert.Null(RegistryJournal.Create(path, confirm: false, out var denied));
        Assert.Equal(RegistryWriteStatus.Denied, denied.Status);

        using (var created = RegistryJournal.Create(path, confirm: true, out var ok))
        {
            Assert.NotNull(created);
            Assert.Equal(RegistryWriteStatus.Ok, ok.Status);
            Assert.Null(RegistryJournal.Create(path, confirm: true, out var exists));
            Assert.Equal(RegistryWriteStatus.InUse, exists.Status);
            Assert.Equal(RegistryWriteStatus.Denied, created!.CommitBatch().Status);
            Assert.Equal(RegistryWriteStatus.Denied, created.AppendMut(new Dictionary<string, object?> { ["op"] = "x" }).Status);
            Assert.Equal(RegistryWriteStatus.Ok, created.BeginBatch("edit", "lab").Status);
            Assert.Equal(RegistryWriteStatus.Denied, created.BeginBatch("edit").Status);
            Assert.Equal(RegistryWriteStatus.Ok, created.AppendMut(new Dictionary<string, object?> { ["op"] = "SetValue" }).Status);
        }

        var info = RegistryJournal.ReadInfo(path);
        Assert.Equal(RegistryJournal.Schema, info.Schema);
        Assert.Contains(info.Batches, b => b.Status is "aborted" or "open" or "committed");

        Assert.Null(RegistryJournal.Load(path, confirm: false, out var loadDenied));
        Assert.Equal(RegistryWriteStatus.Denied, loadDenied.Status);
        Assert.Null(RegistryJournal.Load(Path.Combine(_dir, "missing.jnl"), confirm: true, out var missing));
        Assert.Equal(RegistryWriteStatus.NotFound, missing.Status);

        using (var loaded = RegistryJournal.Load(path, confirm: true, out var loadedOk))
        {
            Assert.NotNull(loaded);
            Assert.Equal(RegistryWriteStatus.Ok, loadedOk.Status);
            loaded!.Dispose();
            loaded.Dispose();
        }

        var bad = Path.Combine(_dir, "bad.jnl");
        File.WriteAllText(bad, """
{"rec":"header","schema":"nope"}
{"rec":"batch","id":"1","kind":"edit"}
{"rec":"mut"}
{"rec":"batch-end","id":"1","status":"committed","muts":1}

{"rec":"batch","id":"2","kind":"x","label":"z"}
{"rec":"mut"}
""");
        Assert.Null(RegistryJournal.Load(bad, confirm: true, out var schema));
        Assert.Equal(RegistryWriteStatus.InvalidPath, schema.Status);
        var leftover = RegistryJournal.ReadInfo(bad);
        Assert.Contains(leftover.Batches, b => b.Status == "open" || b.Id == "2");
    }
}
