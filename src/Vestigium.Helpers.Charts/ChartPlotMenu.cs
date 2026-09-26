using System.Windows.Controls.Primitives;
using ScottPlot;
using ScottPlot.WPF;

namespace Vestigium.Helpers.Charts;

internal sealed class ChartPlotMenu : IPlotMenu
{
    private readonly WpfPlot _view;

    public ChartPlotMenu(WpfPlot view)
    {
        _view = view;
    }

    public List<ContextMenuItem> ContextMenuItems { get; } = [];

    public void Reset() { }

    public void Clear() => ContextMenuItems.Clear();

    public void Add(string Label, Action<Plot> action)
        => ContextMenuItems.Add(new ContextMenuItem { Label = Label, OnInvoke = action });

    public void AddSeparator()
        => ContextMenuItems.Add(new ContextMenuItem { IsSeparator = true });

    public void ShowContextMenu(Pixel pixel)
    {
        var menu = ChartView.CreateHostMenu(_view);
        menu.PlacementTarget = _view;
        menu.Placement = PlacementMode.MousePoint;
        menu.IsOpen = true;
    }
}
