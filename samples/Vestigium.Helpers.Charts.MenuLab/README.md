# Charts Menu Lab

Small WPF host for the DnsIQ chart context menu. Do not pack this project.

## Why it exists

ScottPlot 5 does not use `FrameworkElement.ContextMenu`. Right-click goes through `WpfPlot.Menu` (`WpfPlotMenu`). Setting a WPF `ContextMenu` on the plot does nothing, so DnsIQ kept showing ScottPlot's default items. Those items inherit Vestigium `MenuItem` styles and go white-on-white.

## Run

```powershell
dotnet run --project D:\Source\Clone\Vestigium.Helpers\samples\Vestigium.Helpers.Charts.MenuLab\Vestigium.Helpers.Charts.MenuLab.csproj
```

Or open `Vestigium.Helpers.slnx`, set **Vestigium.Helpers.Charts.MenuLab** as startup, F5.

## What to check

1. Right-click the left plot. That is ScottPlot's own menu.
2. Check **Apply Vestigium-like white MenuItem style**. Right-click the left plot again. Text should vanish.
3. Right-click the right plot. That is `ChartView` `HostMenu`. Text should stay black, and **Show Legend** should be in the list.

When the right plot is readable with the trap on, pack Charts and push. Not before.
