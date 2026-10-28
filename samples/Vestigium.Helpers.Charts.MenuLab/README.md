# Charts Menu Lab

Small WPF host for the DnsIQ chart context menu. Do not pack this project.

## Run

```powershell
dotnet run --project D:\Source\Clone\Vestigium.Helpers\samples\Vestigium.Helpers.Charts.MenuLab\Vestigium.Helpers.Charts.MenuLab.csproj
```

Or open `Vestigium.Helpers.slnx`, set **Vestigium.Helpers.Charts.MenuLab** as startup, F5.

## What to check

1. Pick every Vestigium theme in the combo.
2. Right-click the left plot. That is ScottPlot's own menu. Dark palettes usually make the text vanish.
3. Right-click the right plot. That is `ChartView` `HostMenu`. Text should stay black on white, and **Show Legend** should be in the list.
4. Optional: check **Force white MenuItem style** to isolate implicit style bleed from the theme.

When the right plot stays readable on Standard WPF, Dark Mode, Terminal, and Dracula, pack Charts. Not before.
