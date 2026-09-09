using System.Windows;
using Vestigium.Helpers;
using Vestigium.Helpers.Gallery;

namespace Vestigium.Helpers.Json.Demo;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        HelperWpfHost.Start(this, HelperLog.AppIds.Json);
        new MainWindow().Show();
    }
}
