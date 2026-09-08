using System.Windows;
using Vestigium.Helpers;
using Vestigium.Helpers.Gallery;
using Vestigium.Helpers.WinReg;

namespace Vestigium.Helpers.WinReg.Demo;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        HelperWpfHost.Start(this, HelperLog.AppIds.WinReg);
        new SkeletonWindow
        {
            DataContext = new SkeletonViewModel(new SkeletonSpec
            {
                AppId = HelperLog.AppIds.WinReg,
                Identity = RegistryHelper.Identity,
                Title = "Vestigium.Helpers.WinReg gallery",
                Role = "Windows Registry helpers",
                DocumentId = "VEST-HLP-REG  ·  skeleton",
                Blurb = "Windows-only registry helpers. The library stays on net10.0-windows. This gallery initializes HelperLog with APPID WinReg, then Probe.",
                Probe = RegistryHelper.Probe,
                StatusNote = "Skeleton until its own SRS is accepted. Probe writes Pending then Success through HelperLog.",
            })
        }.Show();
    }
}
