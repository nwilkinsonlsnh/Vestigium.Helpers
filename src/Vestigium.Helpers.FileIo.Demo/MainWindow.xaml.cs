using System.Windows;
using Vestigium.Helpers.Gallery;

namespace Vestigium.Helpers.FileIo.Demo;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = new MainViewModel();
        Closed += (_, _) =>
        {
            if (DataContext is MainViewModel vm)
                vm.Dispose();
            else
                (DataContext as GalleryViewModelBase)?.Dispose();
        };
    }
}
