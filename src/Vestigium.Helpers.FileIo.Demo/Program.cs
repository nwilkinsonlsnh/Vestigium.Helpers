using Vestigium.Helpers;
using Vestigium.Helpers.FileIo;

return HelperDemoHost.Run(
    HelperLog.AppIds.FileIo,
    FileIoHelper.Identity,
    static () => FileIoHelper.Probe());
