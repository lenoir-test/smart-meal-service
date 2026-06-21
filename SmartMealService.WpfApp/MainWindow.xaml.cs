using System.Windows;
using SmartMealService.WpfApp.ViewModels;

namespace SmartMealService.WpfApp;

public partial class MainWindow : Window
{
    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}