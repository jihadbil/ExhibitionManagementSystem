using System.Windows.Controls;
using ExhibitionManagementSystem.DeskTop.ViewModels.Analytics;

namespace ExhibitionManagementSystem.DeskTop.Views.Analytics;

public partial class AnalyticsPage : UserControl
{
    public AnalyticsViewModel ViewModel { get; }

    public AnalyticsPage(AnalyticsViewModel viewModel)
    {
        InitializeComponent();
        ViewModel = viewModel;
        DataContext = ViewModel;

        Loaded += async (s, e) => await ViewModel.OnNavigatedToAsync();
    }
}
