using System.Windows;

namespace Vestigium.Helpers.Gallery;

public partial class SkeletonWindow : Window
{
    public SkeletonWindow()
    {
        InitializeComponent();
        Closed += (_, _) => (DataContext as IDisposable)?.Dispose();
    }
}
