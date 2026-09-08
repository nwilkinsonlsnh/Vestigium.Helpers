using System.Windows;
using Vestigium.Helpers;
using Vestigium.Helpers.Gallery;
using Vestigium.Helpers.Json;

namespace Vestigium.Helpers.Json.Demo;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        HelperWpfHost.Start(this, HelperLog.AppIds.Json);
        new SkeletonWindow
        {
            DataContext = new SkeletonViewModel(new SkeletonSpec
            {
                AppId = HelperLog.AppIds.Json,
                Identity = JsonHelper.Identity,
                Title = "Vestigium.Helpers.Json gallery",
                Role = "System.Text.Json helpers",
                DocumentId = "VEST-HLP-JSON  ·  skeleton",
                Blurb = "System.Text.Json helpers for file and stream payloads. The gallery host initializes logging; the library never calls Initialize.",
                Probe = JsonHelper.Probe,
                StatusNote = "Skeleton until its own SRS is accepted. Probe writes Pending then Success through HelperLog.",
            })
        }.Show();
    }
}
