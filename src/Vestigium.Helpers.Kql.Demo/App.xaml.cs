using System.Windows;
using Vestigium.Helpers;
using Vestigium.Helpers.Gallery;
using Vestigium.Helpers.Kql;

namespace Vestigium.Helpers.Kql.Demo;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        HelperWpfHost.Start(this, HelperLog.AppIds.Kql);
        new SkeletonWindow
        {
            DataContext = new SkeletonViewModel(new SkeletonSpec
            {
                AppId = HelperLog.AppIds.Kql,
                Identity = KqlHelper.Identity,
                Title = "Vestigium.Helpers.Kql gallery",
                Role = "KQL-inspired filter dialect and field catalog",
                DocumentId = "VEST-HLP-KQL  ·  skeleton",
                Blurb = "Filter predicates and grouped counters. The gallery is the host so APPID Kql owns the JSONL folder.",
                Probe = KqlHelper.Probe,
                StatusNote = "Skeleton until Plan Phase 6. Probe writes Pending then Success through HelperLog. No parser yet.",
            })
        }.Show();
    }
}
