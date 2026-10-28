using Vestigium.Helpers.Services;

namespace Vestigium.Helpers.Tests;

[Collection("Logger")]
public sealed class Branch90ServiceControlTests
{
    [Fact]
    public void Protected_and_state_branches_without_mutating()
    {
        Assert.True(ServiceControl.IsProtected("EventLog"));
        Assert.False(ServiceControl.IsProtected("NoSuchService_Vestigium"));

        var start = ServiceHelper.Start("EventLog");
        Assert.True(start.Status is ServiceControlStatus.Ok or ServiceControlStatus.Denied or ServiceControlStatus.Failed);

        Assert.Equal(ServiceControlStatus.Denied, ServiceHelper.Stop("EventLog").Status);
        Assert.Equal(ServiceControlStatus.Denied, ServiceHelper.Restart("EventLog").Status);

        var pause = ServiceHelper.Pause("EventLog");
        Assert.True(pause.Status is ServiceControlStatus.Unsupported or ServiceControlStatus.InvalidState or ServiceControlStatus.Denied or ServiceControlStatus.Ok);
        var cont = ServiceHelper.Continue("EventLog");
        Assert.True(cont.Status is ServiceControlStatus.Unsupported or ServiceControlStatus.InvalidState or ServiceControlStatus.Denied or ServiceControlStatus.Ok);

        Assert.Equal(ServiceControlStatus.Denied, ServiceHelper.SetStartType("EventLog", ServiceStartType.Manual, confirm: true).Status);
        Assert.Equal(ServiceControlStatus.Unsupported, ServiceHelper.SetStartType("Spooler", ServiceStartType.Boot, confirm: true).Status);
        Assert.Equal(ServiceControlStatus.Denied, ServiceHelper.SetStartType("Spooler", ServiceStartType.Manual, confirm: false).Status);

        var missing = ServiceHelper.Start("NoSuchService_Vestigium");
        Assert.Equal(ServiceControlStatus.NotFound, missing.Status);
        Assert.Equal(ServiceControlStatus.NotFound, ServiceHelper.Stop("NoSuchService_Vestigium").Status);
    }
}
