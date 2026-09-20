using System.Windows;
using Vestigium.Helpers.Gallery;

namespace Vestigium.Helpers.Network.Demo;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        HelperWpfHost.Start(this, "Network");
        base.OnStartup(e);
    }
}
