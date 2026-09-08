using System.Windows;
using Vestigium.Helpers;
using Vestigium.Helpers.Gallery;
using Vestigium.Helpers.Csv;

namespace Vestigium.Helpers.Csv.Demo;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        HelperWpfHost.Start(this, HelperLog.AppIds.Csv);
        new SkeletonWindow
        {
            DataContext = new SkeletonViewModel(new SkeletonSpec
            {
                AppId = HelperLog.AppIds.Csv,
                Identity = CsvHelper.Identity,
                Title = "Vestigium.Helpers.Csv gallery",
                Role = "CSV / TSV skeleton — not ClosedXml",
                DocumentId = "VEST-HLP-CSV  ·  skeleton",
                Blurb = "Csv is a sibling of ClosedXml, not a mode of it. ClosedXml must not parse CSV. This gallery only runs Probe until the Csv SRS is accepted.",
                Probe = CsvHelper.Probe,
                StatusNote = "Skeleton until its own SRS is accepted. Probe writes Pending then Success through HelperLog.",
            })
        }.Show();
    }
}
