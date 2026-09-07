using Vestigium.Helpers;
using Vestigium.Helpers.Csv;

return HelperDemoHost.Run(
    HelperLog.AppIds.Csv,
    CsvHelper.Identity,
    static () => CsvHelper.Probe());
