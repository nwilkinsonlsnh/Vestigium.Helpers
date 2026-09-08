using System.Windows;
using Vestigium.Helpers;
using Vestigium.Helpers.Gallery;

namespace Vestigium.Helpers.Charts.Demo;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        HelperWpfHost.Start(this, HelperLog.AppIds.Charts);
        new MainWindow().Show();
    }
}
