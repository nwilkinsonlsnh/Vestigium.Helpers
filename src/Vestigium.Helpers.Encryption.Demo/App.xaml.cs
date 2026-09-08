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
                Role = "AES-256-GCM / ChaCha20-Poly1305 / Argon2id — SRS proposed",
                DocumentId = "VEST-HLP-ENC  ·  SRS proposed",
                Blurb = "Authenticated encryption for strings and large files. AES-256-GCM default, ChaCha20-Poly1305 opt-in, Argon2id for passphrases. Hashing is a sibling library. This gallery only runs Probe until the Encryption SRS is accepted.",
                Probe = EncryptionHelper.Probe,
                StatusNote = "SRS proposed. Probe writes Pending then Success through HelperLog. Implementation follows acceptance.",
            })
        }.Show();
    }
}
