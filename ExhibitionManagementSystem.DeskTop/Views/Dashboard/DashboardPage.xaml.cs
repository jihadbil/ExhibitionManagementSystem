using System.Windows.Controls;
using ExhibitionManagementSystem.DeskTop.ViewModels.Dashboard;

namespace ExhibitionManagementSystem.DeskTop.Views.Dashboard;

public partial class DashboardPage : UserControl
{
    public DashboardViewModel ViewModel { get; }

    public DashboardPage(DashboardViewModel viewModel)
    {
        InitializeComponent();
        ViewModel = viewModel;
        DataContext = ViewModel;

        Loaded += async (s, e) => await ViewModel.OnNavigatedToAsync();
    }
}
