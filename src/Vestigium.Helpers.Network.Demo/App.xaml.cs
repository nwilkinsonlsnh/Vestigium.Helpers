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
        _ = NetworkHelper.Probe();
        new MainWindow().Show();
    }
}
