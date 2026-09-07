using Vestigium.Helpers;
using Vestigium.Helpers.Encryption;

return HelperDemoHost.Run(
    HelperLog.AppIds.Encryption,
    EncryptionHelper.Identity,
    static () => EncryptionHelper.Probe());
