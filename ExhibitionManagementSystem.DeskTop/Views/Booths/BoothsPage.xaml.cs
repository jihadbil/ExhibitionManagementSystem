using System.Windows.Controls;
using ExhibitionManagementSystem.DeskTop.ViewModels.Booths;

namespace ExhibitionManagementSystem.DeskTop.Views.Booths;

public partial class BoothsPage : UserControl
{
    public BoothsViewModel ViewModel { get; }

    public BoothsPage(BoothsViewModel viewModel)
    {
        InitializeComponent();
        ViewModel = viewModel;
        DataContext = ViewModel;

        Loaded += async (s, e) => await ViewModel.OnNavigatedToAsync();
    }
}
