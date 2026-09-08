using System.Windows;
using Vestigium.Helpers;
using Vestigium.Helpers.Gallery;

namespace Vestigium.Helpers.Demo;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        HelperWpfHost.Start(this, HelperLog.AppIds.Core);
        new SkeletonWindow
        {
            DataContext = new SkeletonViewModel(new SkeletonSpec
            {
                AppId = HelperLog.AppIds.Core,
                Identity = HelperGuard.Identity,
                Title = "Vestigium.Helpers gallery",
                Role = "Guards, HelperLog, HelperWpfHost",
                DocumentId = "VEST-HLP-CORE  ·  SRS v1.1",
                Blurb = "Shared guards and the HelperLog façade. Every other helper references this project. This gallery is the host: it initializes logging with APPID Helpers, then Probe writes Pending and Success.",
                Probe = HelperGuard.Probe,
                StatusLabel = "Shipped",
                StatusNote = "Core is shipped. Other helpers stay skeletons until their own SRS is accepted.",
            })
        }.Show();
    }
}
