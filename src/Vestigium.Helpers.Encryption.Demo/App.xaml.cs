using System.Windows;
using Vestigium.Helpers;
using Vestigium.Helpers.Gallery;
using Vestigium.Helpers.Encryption;

namespace Vestigium.Helpers.Encryption.Demo;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        HelperWpfHost.Start(this, HelperLog.AppIds.Encryption);
        new SkeletonWindow
        {
            DataContext = new SkeletonViewModel(new SkeletonSpec
            {
                AppId = HelperLog.AppIds.Encryption,
                Identity = EncryptionHelper.Identity,
                Title = "Vestigium.Helpers.Encryption gallery",
                Role = "Hashing and encryption helpers",
                DocumentId = "VEST-HLP-ENC  ·  skeleton",
                Blurb = "Hashing, symmetric encryption, and secret handling. No custom crypto primitives. This gallery is the host so HelperLog has an APPID before Probe runs.",
                Probe = EncryptionHelper.Probe,
                StatusNote = "Skeleton until its own SRS is accepted. Probe writes Pending then Success through HelperLog.",
            })
        }.Show();
    }
}
