using System.Windows;
using Vestigium.Helpers.Gallery;

namespace Vestigium.Helpers.Kql.Demo;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = new MainViewModel();
        Closed += (_, _) => (DataContext as GalleryViewModelBase)?.Dispose();
    }
}
