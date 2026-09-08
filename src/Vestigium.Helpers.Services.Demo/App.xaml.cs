using System.Windows;
using Vestigium.Helpers;
using Vestigium.Helpers.Gallery;
using Vestigium.Helpers.Services;

namespace Vestigium.Helpers.Services.Demo;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        HelperWpfHost.Start(this, HelperLog.AppIds.Services);
        new SkeletonWindow
        {
            DataContext = new SkeletonViewModel(new SkeletonSpec
            {
                AppId = HelperLog.AppIds.Services,
                Identity = ServiceHelper.Identity,
                Title = "Vestigium.Helpers.Services gallery",
                Role = "Service control helpers",
                DocumentId = "VEST-HLP-SVC  ·  skeleton",
                Blurb = "Service control helpers. Same WPF gallery chrome as Vestigium.Logging — Overview plus a JSONL feed after Probe.",
                Probe = ServiceHelper.Probe,
                StatusNote = "Skeleton until its own SRS is accepted. Probe writes Pending then Success through HelperLog.",
            })
        }.Show();
    }
}
