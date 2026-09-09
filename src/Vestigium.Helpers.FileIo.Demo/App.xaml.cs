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
                Role = "Validated file jobs · recon · UniqueName · Audit Mode",
                DocumentId = "VEST-HLP-FIO  ·  SRS v1.0",
                Blurb = "Validated file and directory jobs. Recon, five buckets, UniqueName default, Audit Mode, Pause/Cancel, ALCOA+ JSONL.",
                Probe = FileIoHelper.Probe,
                StatusNote = "SRS v1.0. Copy / Move / Delete / Mirror. UniqueName default. Probe is %TEMP% only.",
            })
        }.Show();
    }
}
