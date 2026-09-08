using System.Windows;
using Vestigium.Helpers;
using Vestigium.Helpers.Gallery;
using Vestigium.Helpers.Hashing;

namespace Vestigium.Helpers.Hashing.Demo;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        HelperWpfHost.Start(this, HelperLog.AppIds.Hashing);
        new SkeletonWindow
        {
            DataContext = new SkeletonViewModel(new SkeletonSpec
            {
                AppId = HelperLog.AppIds.Hashing,
                Identity = HashingHelper.Identity,
                Title = "Vestigium.Helpers.Hashing gallery",
                Role = "String and file hashing — not Encryption",
                DocumentId = "VEST-HLP-HASH  ·  skeleton",
                Blurb = "Hashing is a sibling of Encryption, not a mode of it. Encryption must not grow Hash APIs. This gallery only runs Probe until the Hashing SRS is accepted.",
                Probe = HashingHelper.Probe,
                StatusNote = "Skeleton until its own SRS is accepted. Probe writes Pending then Success through HelperLog.",
            })
        }.Show();
    }
}
