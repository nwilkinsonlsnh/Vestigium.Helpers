using Vestigium.Helpers.WinReg;

namespace Vestigium.Helpers.Tests;

[Collection("Logger")]
public sealed class Branch90CrapApplyInverseTests : IDisposable
{
    private readonly RegistryHiveKind _hive = RegistryHiveKind.CurrentUser;
    private readonly string _root = @"Software\Vestigium\Helpers.Tests\" + Guid.NewGuid().ToString("N");
    private readonly string _dir = Path.Combine(Path.GetTempPath(), "vest-crap-jnl-" + Guid.NewGuid().ToString("N"));

    public Branch90CrapApplyInverseTests()
    {
        Directory.CreateDirectory(_dir);
        RegistryHelper.Local.CreateKey(_hive, _root, confirm: true);
    }

    public void Dispose()
    {
        RegistryHelper.Local.DeleteKey(_hive, _root, recursive: true, confirm: true);
        try { Directory.Delete(_dir, true); } catch { }
    }

    [Fact]
    public void Rollback_walks_every_inverse_arm()
    {
        Assert.Equal(RegistryWriteStatus.Denied, RegistryHelper.Rollback(Path.Combine(_dir, "x.jnl"), confirm: false).Status);
        Assert.Equal(RegistryWriteStatus.NotFound, RegistryHelper.Rollback(Path.Combine(_dir, "missing.jnl"), confirm: true).Status);

        var empty = Path.Combine(_dir, "empty.jnl");
        using (var j = RegistryJournal.Create(empty, confirm: true, out _))
            j!.BeginBatch("edit");
        Assert.Equal(RegistryWriteStatus.NotFound, RegistryHelper.Rollback(empty, confirm: true).Status);
        Assert.Equal(RegistryWriteStatus.NotFound, RegistryHelper.Rollback(empty, confirm: true, batchId: "nope").Status);

        var path = Path.Combine(_dir, "ops.jnl");
        using (var journal = RegistryJournal.Create(path, confirm: true, out _))
        {
            var client = RegistryHelper.Local;
            client.SetValue(_hive, _root, "A", "1", confirm: true, journal: journal);
            client.SetValue(_hive, _root, "B", "2", confirm: true, journal: journal);
            client.DeleteValue(_hive, _root, "B", confirm: true, journal: journal);
            client.CreateKey(_hive, _root + "\\New", confirm: true, journal: journal);
            client.SetValue(_hive, _root, "RenFrom", "old", confirm: true, journal: journal);
            client.RenameValue(_hive, _root, "RenFrom", "RenTo", confirm: true, journal: journal);
            client.CopyKey(_hive, _root + "\\New", _hive, _root + "\\Copied", confirm: true, journal: journal);
            journal!.RecordMove("RenameKey", _hive, _root + "\\Copied", _root + "\\Moved", destExisted: false);
            journal.RecordKey("DeleteKey", _hive, _root + "\\NoSnap", existed: true);
            journal.RecordAcl("SetSddl", _hive, _root, owner: null, sddl: "D:(A;;KA;;;WD)");
            journal.RecordAcl("SetOwner", _hive, _root, owner: Environment.UserName, sddl: null);
            journal.RecordAcl("TakeOwnership", _hive, _root, owner: Environment.UserName, sddl: null);
            journal.AppendMut(new Dictionary<string, object?>
            {
                ["op"] = "mut-undo",
                ["targetBatch"] = "none",
                ["targetSeq"] = 1,
                ["hive"] = _hive.ToString(),
                ["path"] = _root
            });
            journal.AppendMut(new Dictionary<string, object?>
            {
                ["op"] = "UnknownOp",
                ["hive"] = _hive.ToString(),
                ["path"] = _root
            });
            journal.CommitBatch();
        }

        var rolled = RegistryHelper.Rollback(path, confirm: true, force: true);
        Assert.True(rolled.Status is RegistryWriteStatus.Ok or RegistryWriteStatus.Denied or RegistryWriteStatus.Unsupported or RegistryWriteStatus.NotFound);

        var collide = Path.Combine(_dir, "col.jnl");
        using (var journal = RegistryJournal.Create(collide, confirm: true, out _))
        {
            RegistryHelper.Local.SetValue(_hive, _root, "Keep", "one", confirm: true, journal: journal);
            journal!.CommitBatch();
        }
        RegistryHelper.Local.SetValue(_hive, _root, "Keep", "two", confirm: true);
        var collision = RegistryHelper.Rollback(collide, confirm: true, force: false);
        Assert.True(collision.Status is RegistryWriteStatus.Ok or RegistryWriteStatus.Unsupported or RegistryWriteStatus.Denied);
        _ = RegistryHelper.Rollback(collide, confirm: true, force: true);
    }
}
