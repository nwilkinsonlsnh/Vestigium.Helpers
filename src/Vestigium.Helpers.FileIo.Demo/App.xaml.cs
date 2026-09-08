using System.Windows;
using Vestigium.Helpers;
using Vestigium.Helpers.Gallery;
using Vestigium.Helpers.FileIo;

namespace Vestigium.Helpers.FileIo.Demo;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        HelperWpfHost.Start(this, HelperLog.AppIds.FileIo);
        new SkeletonWindow
        {
            DataContext = new SkeletonViewModel(new SkeletonSpec
            {
                AppId = HelperLog.AppIds.FileIo,
                Identity = FileIoHelper.Identity,
                Title = "Vestigium.Helpers.FileIo gallery",
                Role = "File and directory helpers",
                DocumentId = "VEST-HLP-FIO  ·  skeleton",
                Blurb = "File and directory helpers. Probe is a no-op for JSONL until this gallery calls InitializeHost.",
                Probe = FileIoHelper.Probe,
                StatusNote = "Skeleton until its own SRS is accepted. Probe writes Pending then Success through HelperLog.",
            })
        }.Show();
    }
}
