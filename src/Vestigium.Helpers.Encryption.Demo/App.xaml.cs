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
                Role = "AES-256-GCM / ChaCha20-Poly1305 / Argon2id — nathan.txt → nathan.aes",
                DocumentId = "VEST-HLP-ENC  ·  SRS v1.0",
                Blurb = "Authenticated encryption for UTF-8 strings and large files. AES-256-GCM default, ChaCha20-Poly1305 opt-in, Argon2id for passphrases. Visible suffix is .aes (raw key) or .argon (passphrase). Original name is hidden in the VESTIGIUM TRL trailer. Hashing is a sibling library.",
                Probe = EncryptionHelper.Probe,
                StatusLabel = "Shipped",
                StatusNote = "v1.0 engine. Probe seals and opens in memory. Desktop exports go to Vestigium\\Exports\\Encryption as nathan.aes or nathan.argon. Libraries still never call Initialize.",
            })
        }.Show();
    }
}
