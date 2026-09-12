using System.Windows;

namespace Vestigium.Helpers.WinReg.Demo;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = new MainViewModel();
        Closed += (_, _) => (DataContext as MainViewModel)?.Dispose();
    }
}
