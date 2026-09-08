using System.Windows;
using Vestigium.Helpers;
using Vestigium.Helpers.Gallery;
using Vestigium.Helpers.Xml;

namespace Vestigium.Helpers.Xml.Demo;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        HelperWpfHost.Start(this, HelperLog.AppIds.Xml);
        new SkeletonWindow
        {
            DataContext = new SkeletonViewModel(new SkeletonSpec
            {
                AppId = HelperLog.AppIds.Xml,
                Identity = XmlHelper.Identity,
                Title = "Vestigium.Helpers.Xml gallery",
                Role = "XML document helpers",
                DocumentId = "VEST-HLP-XML  ·  skeleton",
                Blurb = "XML document helpers. Same host pattern as Logging: this process initializes HelperLog, then Probe.",
                Probe = XmlHelper.Probe,
                StatusNote = "Skeleton until its own SRS is accepted. Probe writes Pending then Success through HelperLog.",
            })
        }.Show();
    }
}
