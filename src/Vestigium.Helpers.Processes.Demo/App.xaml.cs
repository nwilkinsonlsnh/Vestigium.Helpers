using System.Windows;
using Vestigium.Helpers;
using Vestigium.Helpers.Gallery;
using Vestigium.Helpers.Processes;

namespace Vestigium.Helpers.Processes.Demo;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        HelperWpfHost.Start(this, HelperLog.AppIds.Processes);
        new SkeletonWindow
        {
            DataContext = new SkeletonViewModel(new SkeletonSpec
            {
                AppId = HelperLog.AppIds.Processes,
                Identity = ProcessHelper.Identity,
                Title = "Vestigium.Helpers.Processes gallery",
                Role = "Process launch and capture",
                DocumentId = "VEST-HLP-PRC  ·  skeleton",
                Blurb = "Process launch and capture helpers. The gallery is the host so APPID Processes owns the JSONL folder.",
                Probe = ProcessHelper.Probe,
                StatusNote = "Skeleton until its own SRS is accepted. Probe writes Pending then Success through HelperLog.",
            })
        }.Show();
    }
}
