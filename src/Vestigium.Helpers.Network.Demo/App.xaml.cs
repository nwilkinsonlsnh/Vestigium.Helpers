using System.Windows;
using Vestigium.Helpers;
using Vestigium.Helpers.Gallery;
using Vestigium.Helpers.Network;

namespace Vestigium.Helpers.Network.Demo;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        HelperWpfHost.Start(this, HelperLog.AppIds.Network);
        new SkeletonWindow
        {
            DataContext = new SkeletonViewModel(new SkeletonSpec
            {
                AppId = HelperLog.AppIds.Network,
                Identity = NetworkHelper.Identity,
                Title = "Vestigium.Helpers.Network gallery",
                Role = "HTTP / socket helpers",
                DocumentId = "VEST-HLP-NET  ·  skeleton",
                Blurb = "HTTP and socket helpers. This gallery initializes HelperLog with APPID Network so Probe has somewhere to write.",
                Probe = NetworkHelper.Probe,
                StatusNote = "Skeleton until its own SRS is accepted. Probe writes Pending then Success through HelperLog.",
            })
        }.Show();
    }
}
